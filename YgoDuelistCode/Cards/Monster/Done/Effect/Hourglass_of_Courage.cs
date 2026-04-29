using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Hourglass_of_Courage : EffectMonsterCard
{
    public Hourglass_of_Courage()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 11,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Normal;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Hourglass_of_Courage),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<HourglassOfCourageHalvedPower>();
        }
    }

    public override async Task OnAfterSummonPipelineAsync(Player player, PlayerChoiceContext ctx, Creature pet, bool canAttackThisTurn)
    {
        if (!canAttackThisTurn && Type == CardType.Attack && !FaceDown)
            await PowerCmd.Apply<HourglassOfCourageHalvedPower>(pet, 2m, player.Creature, this);
    }

    protected override StatEffectTotalMultiplier GetSelfStatMultiplier()
    {
        if (!NormalSummonHalveTimerActive())
            return StatEffectTotalMultiplier.Identity;
        return StatEffectTotalMultiplier.HourglassOfCourageNormalSummon;
    }

    public override void ScheduleFlipFaceUpSideEffectsBeforeFlipPipeline() =>
        ScheduleApplyHalvePowerAfterFlipFaceUp(this);

    private bool NormalSummonHalveTimerActive()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return false;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.HasPower<HourglassOfCourageHalvedPower>();
        }

        return false;
    }

    /// <summary>When a set Hourglass flips face-up, start the halved-ATK/DEF timer (same power as attack normal summon).</summary>
    public static void ScheduleApplyHalvePowerAfterFlipFaceUp(Hourglass_of_Courage card)
    {
        if (card.Owner?.Creature == null)
            return;
        TaskHelper.RunSafely(ApplyHalvePowerAfterFlipFaceUpAsync(card));
    }

    private static async Task ApplyHalvePowerAfterFlipFaceUpAsync(Hourglass_of_Courage card)
    {
        Player? owner = card.Owner;
        if (owner?.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, card))
                continue;
            if (pet.HasPower<HourglassOfCourageHalvedPower>())
                return;
            await PowerCmd.Apply<HourglassOfCourageHalvedPower>(pet, 2m, owner.Creature, card);
            return;
        }
    }
}
