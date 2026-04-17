using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Base for per-monster command option cards. Shares owner and portrait with the source monster card.
/// Needs a parameterless ctor so any reflection-based card scanners don't explode on load.
/// </summary>
public abstract class MonsterCommandCard : CardModel, IYgoCard, ICustomModel
{
    private const int AttributeKeywordBase = 10000;
    private const int RaceKeywordBase = 20000;

    public NormalMonsterCard? SourceMonster { get; private set; }

    /// <summary>
    /// MP: <see cref="SourceMonster"/> is not serialized; stash the field pet's <see cref="Creature.CombatId"/> so peers can
    /// re-bind via <see cref="TryResolveSourceMonsterFromStoredPetId"/> after replication.
    /// </summary>
    public uint SourcePetCombatId { get; private set; }

    /// <summary>
    /// Filled by <c>CardPileCmdStampMonsterCommandManualSourcePilePatch</c> from the pile the card was in
    /// immediately before <see cref="CardPileCmd.AddDuringManualCardPlay"/>; consumed when resolving play.
    /// </summary>
    internal PileType? PendingManualPlaySourcePileType;

    /// <summary>
    /// When false, <see cref="Patches.YgoEnergyIconNodePatch"/> hides the energy orb (menu-only options, not paid with energy).
    /// Ignored when <see cref="CustomCommandEnergyTexturePath"/> is set.
    /// </summary>
    protected internal virtual bool ShowsEnergyCostIcon => true;

    /// <summary>
    /// Optional mod resource path (e.g. <c>YgoDuelist/images/card_frames/foo.png</c>) for the energy orb texture.
    /// When non-null, the orb stays visible and uses this texture instead of the default command styling.
    /// <see cref="Patches.YgoMonsterCommandEnergyCostVisualPatch"/> also applies this texture to the hand unplayable
    /// energy overlay and can blank the <c>0</c> cost label.
    /// </summary>
    protected internal virtual string? CustomCommandEnergyTexturePath => null;

    private protected const float CommandMonsterLevelStripAttributeRightOffsetExtra = 4f;
    private protected const float CommandMonsterLevelStripRaceRightOffsetExtra = 2f;
    private protected const float CommandMonsterLevelStripRaceVerticalOffsetExtra = 73f;

    /// <summary>Extra px for attribute icon horizontal placement (<see cref="Patches.YgoMonsterLevelStripPatch"/>).</summary>
    public virtual float MonsterLevelStripAttributeRightOffsetExtra => CommandMonsterLevelStripAttributeRightOffsetExtra;

    /// <summary>Extra px for race icon horizontal placement (<see cref="Patches.YgoMonsterLevelStripPatch"/>).</summary>
    public virtual float MonsterLevelStripRaceRightOffsetExtra => CommandMonsterLevelStripRaceRightOffsetExtra;

    /// <summary>Extra vertical offset for the race icon row (<see cref="Patches.YgoMonsterLevelStripPatch"/>).</summary>
    public virtual float MonsterLevelStripRaceVerticalOffsetExtra => CommandMonsterLevelStripRaceVerticalOffsetExtra;

    /// <summary>When non-null, <see cref="Patches.MonsterCardRightClickPatch.GetDescriptionLocString"/> uses this for pile display.</summary>
    public virtual LocString? GetPatchedDescriptionLocStringForDisplay() => null;

    /// <summary>
    /// When non-null and <see cref="CustomCommandEnergyTexturePath"/> is empty, <see cref="Patches.YgoEnergyIconNodePatch"/> uses this
    /// prefix with <see cref="MegaCrit.Sts2.Core.Helpers.EnergyIconHelper.GetPath"/> (e.g. <c>"silent"</c> for spell-like commands).
    /// </summary>
    protected internal virtual string? CommandEnergyIconPrefix => null;

    /// <summary>
    /// When true, <see cref="InitializeSource"/> sets <see cref="CardModel.CurrentUpgradeLevel"/> to match
    /// <see cref="SourceMonster"/> so card chrome (title color, frames) reflects whether the field monster is upgraded.
    /// </summary>
    protected virtual bool MirrorSourceMonsterUpgradeVisual => false;

    /// <summary>When true, upgraded cards use <c>.description_upgraded</c> (<see cref="Patches.YgoAlternateUpgradedDescriptionPatch"/>).</summary>
    public virtual bool UseAlternateUpgradedDescription => false;

    /// <summary>Verbose option-pile play lifecycle logging (<see cref="Patches.PlayCardFromOptionPilePatch"/>).</summary>
    internal virtual bool LogsOptionPileLifecycle => false;

    /// <summary>
    /// When <see cref="IsPlayable"/> is false, option-pile play attempts route to
    /// <see cref="YgoDuelist.YgoDuelistCode.GameActions.YgoMonsterMenuCommandNetHelper"/> instead of being ignored.
    /// </summary>
    internal virtual bool TryEnqueueUnplayableOptionPileMenu(Player player, Creature? target) => false;

    /// <summary>
    /// Option-pile plays that defer <c>NCardPlayQueue.OnActionEnqueued</c> (see <see cref="YgoDuelist.YgoDuelistCode.Services.YgoPlayCardQueueDeferral"/>).
    /// </summary>
    internal virtual bool TryGetOptionPilePlayCardQueueDeferral(Player player, out string? reason)
    {
        reason = null;
        return false;
    }

    /// <summary><see cref="Patches.YgoFairyBoxUpkeepTitleUpgradedPatch"/> uses <c>cards/{Id}.title_upgraded</c> when applicable.</summary>
    internal virtual bool ShouldPatchTitleToCardsTitleUpgradedLoc(CardModel self) => false;

    /// <summary><see cref="Patches.CommandChangeBattlePositionTitlePatch"/> and similar; mutates <paramref name="title"/> when returning true.</summary>
    internal virtual bool TryPatchLocalizedTitleForCardModelTitleGetter(CardModel self, ref string title) => false;

    // Parameterless ctor for reflection / scanners – never used at runtime for real commands.
    protected MonsterCommandCard()
        : base(0, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    // Runtime ctor used when we explicitly construct command cards from a monster source.
    protected MonsterCommandCard(NormalMonsterCard source, int cost, CardType type, TargetType target)
        : base(cost, type, CardRarity.Event, target)
    {
        InitializeSource(source);
    }

    public void InitializeSource(NormalMonsterCard source, Creature? pet = null)
    {
        SourceMonster = source;
        if (pet?.CombatId is uint pcid && pcid != 0)
            SourcePetCombatId = pcid;
        else
            SourcePetCombatId = YgoDuelMonsterPetBinding.TryFindPetCombatIdForFieldMonster(source);
        if (MirrorSourceMonsterUpgradeVisual)
            SyncCurrentUpgradeLevelToSourceMonster(source);
        CardModelEnergyCache.Invalidate(this);
    }

    /// <summary>
    /// Rebinds <see cref="SourceMonster"/> from <see cref="SourcePetCombatId"/> when the reference was lost (e.g. MP replication).
    /// </summary>
    public bool TryResolveSourceMonsterFromStoredPetId()
    {
        if (SourceMonster != null)
            return true;
        if (SourcePetCombatId == 0)
            return false;
        Player? player = Owner;
        if (player?.PlayerCombatState == null)
            return false;
        if (YgoDuelMonsterPetBinding.TryGetFieldMonsterForPetCombatId(player, SourcePetCombatId) is NormalMonsterCard nm)
        {
            Creature? pet = null;
            foreach (Creature p in player.PlayerCombatState.Pets)
            {
                if (p.CombatId == SourcePetCombatId)
                {
                    pet = p;
                    break;
                }
            }

            InitializeSource(nm, pet);
            return true;
        }

        return false;
    }

    private void SyncCurrentUpgradeLevelToSourceMonster(NormalMonsterCard source)
    {
        int target = Math.Min(source.CurrentUpgradeLevel, MaxUpgradeLevel);
        if (CurrentUpgradeLevel == target)
            return;
        if (CurrentUpgradeLevel > 0)
            DowngradeInternal();
        for (int i = 0; i < target; i++)
            UpgradeInternal();
        if (target > 0)
            FinalizeUpgradeInternal();
    }

    public YgoCardType YgoCardType => YgoCardType.Spell;

    public DuelMonsterRace DuelMonsterRace
    {
        get
        {
            TryResolveSourceMonsterFromStoredPetId();
            return SourceMonster?.DuelMonsterRace ?? DuelMonsterRace.Warrior;
        }
    }

    // Default to the source monster's portrait if available; otherwise use the generic card back so command cards always have art.
    public override string PortraitPath
    {
        get
        {
            TryResolveSourceMonsterFromStoredPetId();
            return !string.IsNullOrEmpty(SourceMonster?.PortraitPath)
                ? SourceMonster.PortraitPath
                : "card.png".CardImagePath();
        }
    }

    // Use the dedicated command card pool so these menu-only commands render in UIs without falling back to MockCardPool.
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<YgoCommandCardPool>();

    /// <summary>
    /// Base <see cref="CardModel.Pool"/> only resolves if this card's id is listed in a pool's <c>AllCards</c>.
    /// Menu-only commands that are not listed in <see cref="YgoCommandCardPool.AllCards"/> still need a pool; tie <see cref="Pool"/>
    /// to <see cref="VisualCardPool"/> so UI code never hits <see cref="InvalidProgramException"/> ("not in any card pool").
    /// </summary>
    public override CardPoolModel Pool => VisualCardPool;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            TryResolveSourceMonsterFromStoredPetId();
            var source = SourceMonster;
            if (source == null)
                return base.CanonicalKeywords;

            var result = new List<CardKeyword>(4)
            {
                (CardKeyword)(AttributeKeywordBase + (int)source.DuelMonsterAttribute),
                (CardKeyword)(RaceKeywordBase + (int)source.DuelMonsterRace)
            };

            if (source is BaseMonsterCard bm)
            {
                switch (bm.TributeReleaseCount)
                {
                    case 1:
                        result.Add((CardKeyword)20034);
                        break;
                    case 2:
                        result.Add((CardKeyword)20035);
                        break;
                    case >= 3:
                        result.Add((CardKeyword)20047);
                        break;
                }
            }

            foreach (CardKeyword ak in YgoMonsterArchetypeKeywords.KeywordsForMonsterType(source.GetType()))
                result.Add(ak);

            return result;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            TryResolveSourceMonsterFromStoredPetId();
            var source = SourceMonster;
            if (source == null)
                return base.ExtraHoverTips;

            var tips = new List<IHoverTip>(4);
            foreach (var kw in CanonicalKeywords)
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (IHoverTip tip in YgoPreviewReferencedCardTypes.EnumerateHoverTips(GetType()))
                tips.Add(tip);

            return tips;
        }
    }

    /// <summary>
    /// Routes this command card into <see cref="YgoCardOptionPile"/> (same logical result as vanilla
    /// <see cref="CardPileCmd.Add"/> with <see cref="CardPilePosition.Top"/> for a card not yet in a pile).
    /// </summary>
    /// <remarks>
    /// MP: must be <b>synchronous</b>. <see cref="CardPileCmd.Add"/> awaits hooks and tweens; yielding lets the
    /// lockstep queue apply the next action (e.g. <c>Command_Attack</c> play) before <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.NetCombatCardDb"/>
    /// registers these cards — clients then throw "Could not map ID … to any card!".
    /// </remarks>
    public void SendThisCommandToYgoOptionPile()
    {
        var player = Owner;
        if (player == null)
            return;

        var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null)
            return;

        if (Pile != null)
            RemoveFromCurrentPile();

        // Top: same index as CardPileCmd.Add(..., CardPilePosition.Top) → AddInternal(..., 0).
        optionPile.AddInternal(this, 0);
    }

    /// <summary>Core draw / hand / discard / exhaust only — not YGO option row, field, graveyard, etc.</summary>
    internal static bool IsVanillaCombatDeckPile(PileType pileType) =>
        pileType is PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust;

    /// <summary>For <see cref="CardModel.IsPlayable"/> while this instance is still in its source pile.</summary>
    protected bool IsRegularDeckMonsterCommandWithLivePet(Creature? fieldPet) =>
        fieldPet is { IsAlive: true }
        && Pile != null
        && IsVanillaCombatDeckPile(Pile.Type);

    /// <summary>
    /// Clears <see cref="PendingManualPlaySourcePileType"/> and returns whether to skip
    /// <see cref="MonsterCommandRegistry.CommitMonsterCommandAfterPlay"/> (no command slot, no stiff/fatigue from registry).
    /// </summary>
    protected bool TryConsumeRegularDeckCommandWithoutFatigueOrSlots(CardPlay cardPlay, Creature? fieldPet)
    {
        PileType? pending = !cardPlay.IsAutoPlay ? PendingManualPlaySourcePileType : null;
        PendingManualPlaySourcePileType = null;
        if (fieldPet is not { IsAlive: true })
            return false;
        return pending is { } pt && IsVanillaCombatDeckPile(pt);
    }
}
