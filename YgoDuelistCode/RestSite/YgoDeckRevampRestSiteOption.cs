using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.RestSite;

/// <summary>
/// Paid campfire option: consumes the normal rest action and grants +6 bonus YGO deck edit actions.
/// </summary>
public sealed class YgoDeckRevampRestSiteOption(Player owner) : RestSiteOption(owner)
{
    public const string IconResourcePath = "res://YgoDuelist/images/relics/trunk_side_deck.png";

    private static Texture2D? _cachedIcon;

    public static Texture2D? LoadIconTexture()
    {
        _cachedIcon ??= ResourceLoader.Load<Texture2D>(IconResourcePath, null, ResourceLoader.CacheMode.Reuse);
        return _cachedIcon;
    }

    public override string OptionId => "YGO_DECK_REVAMP";

    public override IEnumerable<string> AssetPaths => new[] { IconResourcePath };

    public override Task<bool> OnSelect()
    {
        YgoCampfireDeckEditCharges.GrantDeckRevampBonus(Owner);
        return Task.FromResult(true);
    }
}
