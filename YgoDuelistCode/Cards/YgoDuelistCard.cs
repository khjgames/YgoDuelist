using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using YgoDuelist.YgoDuelistCode.Character;
using YgoDuelist.YgoDuelistCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards;

[Pool(typeof(YgoDuelistCardPool))]
public abstract class YgoDuelistCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
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
    public virtual Type[] RelatedCards => Array.Empty<Type>(); // Every other card is weighted at 1, these are weighted at 2.

    /// <summary>
    /// When true, <see cref="BundledCards"/> may list this card's own type for one extra copy (merchant grant, pack mate, shop stack preview).
    /// When false, same-id bundle entries are skipped so anchors can appear in <see cref="BundledCards"/> for other reasons without a duplicate grant.
    /// </summary>
    public virtual bool BundleGrantsExtraCopyOfSelf => false;

    /// <summary>
    /// When true, upgraded cards (and upgrade preview) use <c>cards.json</c> key <c>.description_upgraded</c> instead of <c>.description</c>.
    /// </summary>
    public virtual bool UseAlternateUpgradedDescription => false;

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
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190

    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    public override string BetaPortraitPath => $"beta/{Id.Entry.ToLowerInvariant()}.png".CardImagePath();

    /// <summary>CLR types for cards named in quotes in localization; fusion/ritual subclasses filter duplicates.</summary>
    protected virtual IEnumerable<Type> EnumerateReferencedCardPreviewTypes()
    {
        foreach (Type t in YgoReferencedCardPreviewMap.GetReferencedTypes(GetType()))
        {
            if (t != null)
                yield return t;
        }
    }

    protected IEnumerable<IHoverTip> EnumerateReferencedCardPreviewHoverTips()
    {
        foreach (Type t in EnumerateReferencedCardPreviewTypes())
            yield return HoverTipFactory.FromCard(YgoPackCardCatalog.CardFromType(t));
    }
}