using System;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using YgoDuelist.YgoDuelistCode.Character;
using YgoDuelist.YgoDuelistCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards;

[Pool(typeof(YgoDuelistCardPool))]
public abstract class YgoDuelistCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target),
    IYgoNHandPlayPhaseHighlightOverride
{
    public virtual YgoCardPackTags PackTags => YgoCardPackTags.None;

    /// <summary>
    /// Flat gold added to (or subtracted from) this card's YGO merchant price after rarity base cost, before shop jitter
    /// (<see cref="Patches.PatchesForMerchant.YgoMerchantCalcCostPatch"/>). Vanilla non-YGO shops ignore this.
    /// </summary>
    public virtual int ShopPriceModifier => 0;

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks (<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public virtual float PackWeightMultiplier => 1f;

    public virtual Type[] BundledCards => Array.Empty<Type>();

    /// <summary>
    /// Pack-weighting only: other card types that get increased weight when this card is kept. Not used for UI previews.
    /// </summary>
    public virtual Type[] RelatedCards => Array.Empty<Type>(); // Every other card is weighted at 1, these are weighted at 2.

    /// <summary>Optional named archetype groups for <see cref="GetRelatedCards"/> (merged with implicit archetypes from <see cref="YgoCardArchetypeRegistry"/>).</summary>
    public virtual YgoCardArchetype CardArchetypes => YgoCardArchetype.None;

    /// <summary>
    /// Pack-weighting related pool: merges <paramref name="explicitAdditional"/>, archetype members, fusion/ritual/double-tribute links.
    /// </summary>
    protected Type[] GetRelatedCards(params Type[]? explicitAdditional) =>
        YgoRelatedCardsComposer.Compose(this, fusionSeed: null, explicitAdditional);

    /// <summary>
    /// Full set of referenced card types shown as hover previews. Default: localization quotes only
    /// (<see cref="YgoPreviewReferencedCardTypes.FromLocalizationQuotes"/>). Override with
    /// <see cref="YgoPreviewReferencedCardTypes.Merged"/> to add tokens/materials, or a fixed array to replace quotes entirely.
    /// </summary>
    protected virtual Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.FromLocalizationQuotes(GetType());

    /// <summary>
    /// When true, <see cref="BundledCards"/> may list this card's own type for one extra copy (merchant grant, pack mate, shop stack preview).
    /// When false, same-id bundle entries are skipped so anchors can appear in <see cref="BundledCards"/> for other reasons without a duplicate grant.
    /// </summary>
    public virtual bool BundleGrantsExtraCopyOfSelf => false;

    /// <summary>
    /// When true, pack rewards may add tag-matched same-rarity bonus cards (<see cref="YgoDuelist.YgoDuelistCode.Services.YgoBulkBundledResolver"/>),
    /// and the YGO merchant shows a stacked preview + extra grant like explicit <see cref="BundledCards"/>.
    /// </summary>
    public virtual bool BulkBundled => false;

    /// <summary>
    /// When true, upgraded cards (and upgrade preview) use <c>cards.json</c> key <c>.description_upgraded</c> instead of <c>.description</c>.
    /// </summary>
    public virtual bool UseAlternateUpgradedDescription => false;

    /// <summary>
    /// When non-null, <see cref="Patches.YgoEnergyIconNodePatch"/> uses this texture for the character strike/defend stubs instead of default energy styling.
    /// </summary>
    public virtual string? YgoStrikeDefendEnergyIconTexturePath => null;

    /// <summary>
    /// When true, <see cref="GetCombatHandDescriptionLocString"/> uses <c>.description_combat</c> in combat hand (like monster attack/skill combat keys), else <c>.description</c>.
    /// </summary>
    public virtual bool UsesCombatHandDescription => false;

    /// <summary>
    /// When set, <see cref="YgoDuelist.YgoDuelistCode.Patches.YgoDuelistCustomFrameHsvPatch"/> replaces the pool frame shader with this H/S/V (same convention as <see cref="Character.YgoDuelistCardPool"/>).
    /// </summary>
    public virtual (float H, float S, float V)? CustomFrameTintHsv => null;

    /// <summary>Spell/trap cards: show Splinter keyword chip when card text references Splinter mechanics.</summary>
    public virtual bool CardShowsSplinterKeyword => false;

    /// <summary>Spell/trap cards: show Blight keyword chip when card text references Blight.</summary>
    public virtual bool CardShowsBlightKeyword => false;

    /// <summary>Spell/trap cards: show Reckless keyword chip (e.g. equips that grant combat self-damage on the monster).</summary>
    public virtual bool CardShowsRecklessKeyword => false;

    /// <summary>
    /// When non-null in the Spell/Trap zone (face-up), energy UI uses this texture for the orb overlay
    /// (<see cref="Patches.YgoMonsterCommandEnergyCostVisualPatch"/>).
    /// </summary>
    public virtual string? GetSpellTrapZoneFaceUpEnergyOrbOverridePath(CardPile? pile) => null;

    /// <summary>
    /// After vanilla <see cref="CardModel.IsValidTarget"/> yields <paramref name="vanillaResult"/>, optionally narrow the result
    /// (e.g. attack-intent thresholds). Default: unchanged. Dispatched from <c>CardModel.IsValidTarget</c> postfix — keep logic on the card, not in patches.
    /// </summary>
    public virtual bool RefineIsValidTarget(Creature? target, bool vanillaResult) => vanillaResult;

    /// <summary>
    /// When non-null during combat play phase, replaces the vanilla cyan playable outline on <see cref="NHandCardHolder"/>.
    /// Default: no override. Non-<see cref="YgoDuelistCard"/> models use <see cref="IYgoNHandPlayPhaseHighlightOverride"/> directly.
    /// </summary>
    public virtual Color? GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight) => null;

    Color? IYgoNHandPlayPhaseHighlightOverride.GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight) =>
        GetNHandPlayPhaseHighlightModulateOverride(holder, vanillaWouldUseCyanPlayableHighlight);

    /// <summary>
    /// When true, <see cref="Powers.YgoTemporaryThornsPower"/> may reflect damage even if the hit was flagged unpowered.
    /// </summary>
    public virtual bool TemporaryThornsReflectsUnpoweredDamage => false;

    /// <summary>
    /// Resolves <c>description</c> vs <c>description_combat</c> and optional <c>_upgraded</c> suffixes; used by <see cref="Patches.MonsterCardRightClickPatch.GetDescriptionLocString"/>.
    /// </summary>
    public LocString GetCombatHandDescriptionLocString()
    {
        string suffix = ".description";
        if (IsInHandDuringCombat())
            suffix = ".description_combat";

        if (UseAlternateUpgradedDescription
            && (IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None))
        {
            var upgraded = new LocString("cards", Id.Entry + suffix + "_upgraded");
            if (upgraded.Exists())
                return upgraded;
        }

        return new LocString("cards", Id.Entry + suffix);
    }

    private bool IsInHandDuringCombat()
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return false;
        return Pile?.Type == PileType.Hand;
    }

    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    /// <summary>Large card art: prefer <c>card_portraits/big/</c>; if missing, use same filename under <c>card_portraits/</c> (BaseLib loads this for <see cref="CardModel.Portrait"/>).</summary>
    public override string CustomPortraitPath
    {
        get
        {
            string fileName = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png";
            string bigPath = fileName.BigCardImagePath();
            if (ResourceLoader.Exists(bigPath))
                return bigPath;
            return fileName.CardImagePath();
        }
    }

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190

    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.ToLowerInvariant()}.png".CardImagePath();

    /// <summary>Yields <see cref="PreviewReferencedCardTypes"/> (excluding this card’s type). Subclasses may override to filter.</summary>
    protected virtual IEnumerable<Type> EnumerateReferencedCardPreviewTypes()
    {
        Type host = GetType();
        foreach (Type t in PreviewReferencedCardTypes)
        {
            if (t != null && t != host)
                yield return t;
        }
    }

    protected IEnumerable<IHoverTip> EnumerateReferencedCardPreviewHoverTips()
    {
        foreach (Type t in EnumerateReferencedCardPreviewTypes())
            yield return HoverTipFactory.FromCard(YgoPackCardCatalog.CardFromType(t));
    }
}