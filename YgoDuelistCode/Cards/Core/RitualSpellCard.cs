using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Ritual spell: summons a <see cref="RitualMonsterCard"/> from the hand using hand/field materials whose total level matches the spell rules.</summary>
public abstract class RitualSpellCard : BaseSpellCard
{
    public Type RitualTargetMonsterType { get; }
    public int LevelRequirement { get; }
    public RitualMaterialLevelCompare MaterialLevelCompare { get; }

    /// <summary>
    /// When false (default): if exactly one valid ritual monster is in hand, it is used automatically; multiple copies open the picker.
    /// When true: the ritual target grid always opens (e.g. generic ritual spells with several different valid monster types).
    /// </summary>
    public bool RequiresPlayerRitualTargetSelection { get; }

    /// <summary>When set, only ritual monsters with this attribute are valid targets (e.g. DARK for Contract with the Abyss).</summary>
    public DuelMonsterAttribute? RitualTargetAttributeFilter { get; }

    /// <summary>When true, material Levels must sum exactly to the chosen ritual monster's Level (overrides <see cref="LevelRequirement"/> and <see cref="MaterialLevelCompare"/> for materials).</summary>
    public bool UseRitualTargetLevelAsMaterialRequirement { get; }

    /// <summary>When 1, exactly one Tribute monster may be selected (Black Illusion Ritual).</summary>
    public int? ExactMaterialCardCount { get; }

    protected RitualSpellCard(
        int cost,
        CardRarity rarity,
        TargetType target,
        DuelMonsterRace spellRace,
        Type ritualTargetMonsterType,
        int levelRequirement,
        RitualMaterialLevelCompare materialLevelCompare,
        bool requiresPlayerRitualTargetSelection = false,
        DuelMonsterAttribute? ritualTargetAttributeFilter = null,
        bool useRitualTargetLevelAsMaterialRequirement = false,
        int? exactMaterialCardCount = null)
        : base(cost, rarity, target, spellRace)
    {
        ArgumentNullException.ThrowIfNull(ritualTargetMonsterType);
        if (!typeof(RitualMonsterCard).IsAssignableFrom(ritualTargetMonsterType))
            throw new ArgumentException("Ritual target type must be a RitualMonsterCard.", nameof(ritualTargetMonsterType));

        RitualTargetMonsterType = ritualTargetMonsterType;
        LevelRequirement = levelRequirement;
        MaterialLevelCompare = materialLevelCompare;
        RequiresPlayerRitualTargetSelection = requiresPlayerRitualTargetSelection;
        RitualTargetAttributeFilter = ritualTargetAttributeFilter;
        UseRitualTargetLevelAsMaterialRequirement = useRitualTargetLevelAsMaterialRequirement;
        ExactMaterialCardCount = exactMaterialCardCount;
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;
            bool inPlayablePile = Pile?.Type == PileType.Hand || Pile?.Type == SpellTrapZonePile.CustomType;
            if (Owner == null || CombatManager.Instance?.IsInProgress != true || !inPlayablePile)
                return true;
            if (RitualSummonSelection.IsCompletingRitualSpellPlay)
                return true;
            return RitualSummonSelection.HasFeasibleRitualPlay(Owner, this);
        }
    }

    /// <summary>
    /// Ritual materials + summon run here (after cast anim, before the spell card is sent to the graveyard).
    /// Per-card spell text: override <see cref="OnRitualSpellAfterResolution"/> instead of <see cref="OnSpellPlay"/>.
    /// </summary>
    protected sealed override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != null
            && RitualSpellPlayPayload.TryTakePending(this, out RitualSpellPendingResolution? pending)
            && pending != null)
        {
            await RitualSummonSelection.ApplyResolvedRitualAsync(Owner, pending, choiceContext);
        }

        await OnRitualSpellAfterResolution(choiceContext, cardPlay);
    }

    /// <summary>Optional spell text after ritual resolution; default is no-op.</summary>
    protected virtual Task OnRitualSpellAfterResolution(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Task.CompletedTask;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var tip in base.ExtraHoverTips)
                yield return tip;

            var monsterPreview = BuildNamedRitualTargetMonsterPreviewTip();
            if (monsterPreview != null)
                yield return monsterPreview;
        }
    }

    private IHoverTip? BuildNamedRitualTargetMonsterPreviewTip()
    {
        Type targetType = RitualTargetMonsterType;
        if (!targetType.IsSubclassOf(typeof(RitualMonsterCard)) || targetType.IsAbstract)
            return null;

        CardModel template = YgoPackCardCatalog.CardFromType(targetType);
        return HoverTipFactory.FromCard(template);
    }
}
