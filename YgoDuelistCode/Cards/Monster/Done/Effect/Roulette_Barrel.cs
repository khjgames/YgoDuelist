using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Activate Effect: roll a {Mgc}-sided die and deal that much damage to the selected enemy.</summary>
public sealed class Roulette_Barrel : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString RollPreviewPrompt = new("cards", "YGODUELIST-ROULETTE_BARREL.roll_result.selection");

    public Roulette_Barrel()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 10,
            baseDef: 20,
            baseMgc: 6,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Machine | YgoCardPackTags.Chance | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Roulette_Barrel) };

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Attack;

    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-ROULETTE_BARREL.activated_effect.description";

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.Creature?.CombatState is not { } cs)
            return;
        if (cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        int sides = Math.Max(1, (int)source.DynamicVars["Mgc"].BaseValue);
        ulong mix = YgoDeterministicRng.MixDuelMonsterAttack(player.Creature, pet, cardPlay);
        int roll = YgoDeterministicRng.RollDie(cs, sides, "ROULETTE_BARREL-DIE", mix);

        await YgoPreviewGridSelection.ShowPreviewAsync(
            choiceContext,
            new List<CardModel> { YgoDeterministicRngResultDisplay.CreateD6RollResultCard(cs, player, Math.Clamp(roll, 1, 6)) },
            player,
            RollPreviewPrompt);

        if (roll > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                cardPlay.Target,
                roll,
                ValueProp.Move,
                dealer: pet,
                cardSource: source);
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }
}
