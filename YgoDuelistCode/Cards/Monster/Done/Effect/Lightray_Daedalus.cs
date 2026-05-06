using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: Special Summon if 4+ LIGHT monsters are in your Graveyard.
/// Gains {Mgc} ATK for each LIGHT monster in your Graveyard.
/// Activate (once per turn): destroy 1 face-up Field Spell on the field, then destroy 2 other face-up duel monsters on the field.
/// </summary>
public sealed class Lightray_Daedalus : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString PickFieldSpellPrompt =
        new("cards", "YGODUELIST-LIGHTRAY_DAEDALUS.pick_field_spell");

    private static readonly LocString PickMonstersPrompt =
        new("cards", "YGODUELIST-LIGHTRAY_DAEDALUS.pick_two_monsters");

    public Lightray_Daedalus()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 26,
            baseDef: 15,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.SeaSerpent)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Light | YgoCardPackTags.Water | YgoCardPackTags.Ocean;

    public override Type[] RelatedCards => new[] { typeof(Lightray_Daedalus), typeof(Levia_Dragon_Daedalus) };

    protected override bool SupportsHandEffectForm => true;

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();
        int n = CountLightMonstersInGraveyard(Owner);
        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (Math.Max(0, n * mgc), 0);
    }

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanResolveHandSpecialSummon(Owner));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            Player? player = Owner;
            if (player == null || !CanResolveHandSpecialSummon(player))
                return;
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-LIGHTRAY_DAEDALUS.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            if (pet == null || !MonsterCommandRegistry.TryGet(pet, out var cmd) || cmd.HasUsedActivatedEffectThisTurn)
                return false;
            CombatState cs = Owner.Creature.CombatState;
            return BuildFaceUpFieldSpells(cs).Count >= 1
                && BuildFaceUpFieldMonstersExcluding(cs, this).Count >= 2;
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Lightray_Daedalus || Owner?.Creature?.CombatState == null)
            return;

        Player player = Owner;
        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null)
            return;

        CombatState cs = player.Creature!.CombatState!;

        BaseFieldSpellCard? fieldSpell = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickFieldSpellPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildFaceUpFieldSpells(cs));
        if (fieldSpell == null)
            return;

        List<BaseMonsterCard> monsters = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickMonstersPrompt, 2, 2)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildFaceUpFieldMonstersExcluding(cs, this),
            maxResults: 2);
        if (monsters.Count < 2)
            return;

        if (!await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(fieldSpell, this))
            return;

        foreach (BaseMonsterCard m in monsters)
        {
            (Player fieldOwner, Creature pet)? row = FindPetForFieldMonster(cs, m);
            if (row == null)
                continue;
            await YgoDuelMonsterDestructionRules.KillPetWithinDestructionAsync(
                YgoDestructionSourceKind.MonsterEffect,
                row.Value.pet);
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
    }

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return CountLightMonstersInGraveyard(player) >= 4
            && ReactorSlimeSummonGate.AllowsSummonPrintedRace(player, DuelMonsterRace.SeaSerpent);
    }

    private static int CountLightMonstersInGraveyard(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Count(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Light);

    private static List<BaseFieldSpellCard> BuildFaceUpFieldSpells(CombatState cs)
    {
        var list = new List<BaseFieldSpellCard>();
        foreach (Player pl in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
            list.AddRange(YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(pl));
        return list;
    }

    private static List<BaseMonsterCard> BuildFaceUpFieldMonstersExcluding(CombatState cs, BaseMonsterCard exclude)
    {
        var list = new List<BaseMonsterCard>();
        foreach (Player pl in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            if (pl.PlayerCombatState == null)
                continue;
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(pl.PlayerCombatState))
            {
                if (!pet.IsAlive)
                    continue;
                BaseMonsterCard? m = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
                if (m == null || m.FaceDown || ReferenceEquals(m, exclude))
                    continue;
                if (!YgoDuelMonsterDestructionRules
                        .FilterFieldMonstersForDestroySelection(
                            pl,
                            new[] { m },
                            YgoDestructionSourceKind.MonsterEffect)
                        .Any())
                    continue;
                list.Add(m);
            }
        }

        return list;
    }

    private static (Player fieldOwner, Creature pet)? FindPetForFieldMonster(CombatState cs, BaseMonsterCard card)
    {
        foreach (Player pl in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            if (pl.PlayerCombatState == null)
                continue;
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(pl.PlayerCombatState))
            {
                if (!pet.IsAlive)
                    continue;
                BaseMonsterCard? m = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
                if (m != null && ReferenceEquals(m, card))
                    return (pl, pet);
            }
        }

        return null;
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
