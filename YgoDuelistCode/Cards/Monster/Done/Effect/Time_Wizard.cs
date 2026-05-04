using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Activate Effect (once per turn): call Heads or Tails, then toss a coin.
/// Success: all enemies take damage equal to the total ATK of monsters you control.
/// Fail: destroy all your duel monsters; your leader takes damage equal to {Mgc}% of the total ATK those monsters had (base 50%, upgraded 40%).
/// </summary>
public sealed class Time_Wizard : EffectMonsterCard, IMonsterActivatedEffect
{
    private const string CoinSalt = "TIME_WIZARD-COIN";

    public Time_Wizard()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 5,
            baseDef: 4,
            baseMgc: 50,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Chance;

    public override Type[] RelatedCards =>
        new[]
        {
            typeof(Time_Wizard),
            typeof(Fairy_Box),
            typeof(Jirai_Gumo),
            typeof(Copycat),
            typeof(Blowback_Dragon),
            typeof(Barrel_Dragon),
            typeof(Gatling_Dragon),
            typeof(Goddess_of_Whim),
            typeof(Baby_Dragon),
            typeof(Thousand_Dragon),
            typeof(Dark_Magician),
            typeof(Dark_Sage),
        };

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-TIME_WIZARD.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            if (pet == null || !pet.IsAlive)
                return false;
            return MonsterCommandRegistry.TryGet(pet, out MonsterCommandState cmd)
                && !cmd.HasUsedActivatedEffectThisTurn;
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Time_Wizard || Owner?.Creature?.CombatState is not CombatState cs)
            return;

        Player player = Owner;
        if (player.PlayerCombatState == null || player.Creature == null)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || !selfPet.IsAlive)
            return;

        CardModel headsCall = cs.CreateCard<Heads>(player);
        CardModel tailsCall = cs.CreateCard<Tails>(player);
        var coinOptions = new List<CardModel> { headsCall, tailsCall };

        CardModel? callPick = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            coinOptions,
            player,
            canSkip: false);

        if (callPick == null)
            return;

        bool calledHeads = callPick.Id.Entry == headsCall.Id.Entry;
        ulong mix = YgoDeterministicRng.MixDuelMonsterAttack(player.Creature, selfPet, cardPlay);
        bool flipIsHeads = YgoDeterministicRng.CoinFlip(cs, CoinSalt, mix);

        CardModel resultCard = YgoDeterministicRngResultDisplay.CreateCoinFlipResultCard(cs, player, flipIsHeads);
        var coinPrompt = new LocString("cards", "YGODUELIST-TIME_WIZARD.coin_result.selection");
        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, new List<CardModel> { resultCard }, player, coinPrompt);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);

        bool success = calledHeads == flipIsHeads;
        List<BaseMonsterCard> fieldSnapshot = DuelMonsterFieldRegistry.OrderedFieldMonsters(player).ToList();

        if (success)
        {
            foreach (BaseMonsterCard m in fieldSnapshot)
            {
                if (m is Dark_Magician dm)
                    dm.SurvivedTimeMagic = true;
            }

            int totalAtk = fieldSnapshot.Sum(m => m.CalcDuelMonsterStats(fieldSnapshot).Atk);
            if (totalAtk <= 0 || player.Creature == null)
                return;

            decimal dmg = totalAtk;
            foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
            {
                await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, player.Creature, source);
            }

            return;
        }

        List<Creature> duelPets = YgoMpCombatOrder
            .PetsSnapshotOrderedByCombatId(player.PlayerCombatState)
            .Where(p => p.IsAlive && p.Monster is DuelMonsterModel)
            .ToList();
        List<Creature> toKill = YgoDuelMonsterDestructionRules
            .FilterPetsForMassKill(duelPets, YgoDestructionSourceKind.MonsterEffect)
            .ToList();

        int totalDestroyedAtk = 0;
        foreach (Creature pet in toKill)
        {
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is BaseMonsterCard m)
                totalDestroyedAtk += m.CalcDuelMonsterStats(fieldSnapshot).Atk;
        }

        foreach (Creature pet in toKill)
        {
            if (!pet.IsAlive)
                continue;
            await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
                YgoDestructionSourceKind.MonsterEffect,
                pet);
        }

        decimal pct = source.DynamicVars["Mgc"].BaseValue / 100m;
        decimal heroDmg = totalDestroyedAtk * pct;
        if (heroDmg > 0m && player.Creature != null)
        {
            await CreatureCmd.Damage(
                choiceContext,
                player.Creature,
                heroDmg,
                ValueProp.Unpowered,
                player.Creature,
                source);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 40m;
    }
}
