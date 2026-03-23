using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Fusion spell: Fusion Summon from the Extra Deck using hand/field materials that match the fusion monster's recipe.</summary>
public abstract class FusionSpellCard : BaseSpellCard
{
    /// <summary>Valid fusion monsters must be assignable to this type (use <see cref="FusionMonsterCard"/> for generic Polymerization-style spells).</summary>
    public Type FusionTargetMonsterType { get; }

    /// <summary>When false: single valid fusion in Extra Deck is auto-chosen; when true or multiple candidates, open the picker.</summary>
    public bool RequiresPlayerFusionTargetSelection { get; }

    protected FusionSpellCard(
        int cost,
        CardRarity rarity,
        TargetType target,
        DuelMonsterRace spellRace,
        Type fusionTargetMonsterType,
        bool requiresPlayerFusionTargetSelection = false)
        : base(cost, rarity, target, spellRace)
    {
        ArgumentNullException.ThrowIfNull(fusionTargetMonsterType);
        if (!typeof(FusionMonsterCard).IsAssignableFrom(fusionTargetMonsterType))
            throw new ArgumentException("Fusion target type must be a FusionMonsterCard.", nameof(fusionTargetMonsterType));

        FusionTargetMonsterType = fusionTargetMonsterType;
        RequiresPlayerFusionTargetSelection = requiresPlayerFusionTargetSelection;
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;
            if (Owner == null || CombatManager.Instance?.IsInProgress != true || Pile?.Type != PileType.Hand)
                return true;
            if (FusionSummonSelection.IsCompletingFusionSpellPlay)
                return true;
            return FusionSummonSelection.HasFeasibleFusionPlay(Owner, this);
        }
    }

    protected sealed override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null
            && FusionSpellPlayPayload.TryTakePending(this, out FusionSpellPendingResolution? pending)
            && pending != null)
        {
            await FusionSummonSelection.ApplyResolvedFusionAsync(Owner, pending, choiceContext);
        }

        await OnFusionSpellAfterResolution(choiceContext, cardPlay);
    }

    protected virtual Task OnFusionSpellAfterResolution(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Task.CompletedTask;
}
