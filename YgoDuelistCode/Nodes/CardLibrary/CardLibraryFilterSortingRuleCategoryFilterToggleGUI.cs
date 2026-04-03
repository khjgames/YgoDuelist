using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>
/// Wraps a vanilla card-library tickbox control so custom filter rows can reuse the same visuals.
/// </summary>
public sealed class CardLibraryFilterSortingRuleCategoryFilterToggleGUI
{
    private readonly CardLibraryFilterToggleStyle _style;
    private readonly NCardTypeTickbox? _type;
    private readonly NCardRarityTickbox? _rarity;
    private readonly NCardCostTickbox? _cost;

    private CardLibraryFilterSortingRuleCategoryFilterToggleGUI(
        CardLibraryFilterToggleStyle style,
        NCardTypeTickbox? type,
        NCardRarityTickbox? rarity,
        NCardCostTickbox? cost)
    {
        _style = style;
        _type = type;
        _rarity = rarity;
        _cost = cost;
    }

    public Control Root => _style switch
    {
        CardLibraryFilterToggleStyle.CardType => _type!,
        CardLibraryFilterToggleStyle.Rarity => _rarity!,
        CardLibraryFilterToggleStyle.Cost => _cost!,
        _ => throw new ArgumentOutOfRangeException()
    };

    public bool IsTicked
    {
        get => _style switch
        {
            CardLibraryFilterToggleStyle.CardType => _type!.IsTicked,
            CardLibraryFilterToggleStyle.Rarity => _rarity!.IsTicked,
            CardLibraryFilterToggleStyle.Cost => _cost!.IsTicked,
            _ => throw new ArgumentOutOfRangeException()
        };
        set
        {
            switch (_style)
            {
                case CardLibraryFilterToggleStyle.CardType:
                    _type!.IsTicked = value;
                    break;
                case CardLibraryFilterToggleStyle.Rarity:
                    _rarity!.IsTicked = value;
                    break;
                case CardLibraryFilterToggleStyle.Cost:
                    _cost!.IsTicked = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void ConnectChanged(Action onChanged)
    {
        switch (_style)
        {
            case CardLibraryFilterToggleStyle.CardType:
                _type!.Connect(NCardTypeTickbox.SignalName.Toggled, Callable.From<NCardTypeTickbox>(_ => onChanged()));
                break;
            case CardLibraryFilterToggleStyle.Rarity:
                _rarity!.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(_ => onChanged()));
                break;
            case CardLibraryFilterToggleStyle.Cost:
                _cost!.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => onChanged()));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    static readonly LocString PackTagRowHoverLoc = new("static_hover_tips", "PACK_TAG_FILTER_ROW");

    /// <param name="labelText">Rarity/Cost: shown text. CardType: unused when a custom icon is supplied.</param>
    /// <param name="iconTexture">CardType only: replaces the default %Image texture when set (expected pre-scaled, e.g. 5%).</param>
    /// <param name="hoverLoc">Hover tip for this row; when null, uses the generic <c>PACK_TAG_FILTER_ROW</c> entry.</param>
    public static CardLibraryFilterSortingRuleCategoryFilterToggleGUI Create(
        CardLibraryFilterToggleStyle style,
        string labelText,
        Texture2D? iconTexture = null,
        LocString? hoverLoc = null)
    {
        switch (style)
        {
            case CardLibraryFilterToggleStyle.CardType:
            {
                var path = SceneHelper.GetScenePath("screens/card_library/card_type_tickbox");
                var node = PreloadManager.Cache.GetScene(path).Instantiate<NCardTypeTickbox>();
                if (iconTexture != null)
                    node.GetNode<TextureRect>("%Image").Texture = iconTexture;
                node.Loc = hoverLoc ?? PackTagRowHoverLoc;
                return new CardLibraryFilterSortingRuleCategoryFilterToggleGUI(style, node, null, null);
            }
            case CardLibraryFilterToggleStyle.Rarity:
            {
                var path = SceneHelper.GetScenePath("screens/card_library/rarity_tickbox");
                var node = PreloadManager.Cache.GetScene(path).Instantiate<NCardRarityTickbox>();
                // Same as NCardViewSortButton: SetLabel uses _label from _Ready() which has not run off-tree.
                node.GetNode<MegaLabel>("Label").SetTextAutoSize(labelText);
                node.Loc = hoverLoc ?? PackTagRowHoverLoc;
                return new CardLibraryFilterSortingRuleCategoryFilterToggleGUI(style, null, node, null);
            }
            case CardLibraryFilterToggleStyle.Cost:
            {
                var path = SceneHelper.GetScenePath("screens/card_library/card_cost_tickbox");
                var node = PreloadManager.Cache.GetScene(path).Instantiate<NCardCostTickbox>();
                var label = node.GetNode<MegaLabel>("%Label");
                label.SetTextAutoSize(labelText);
                node.Loc = hoverLoc ?? PackTagRowHoverLoc;
                return new CardLibraryFilterSortingRuleCategoryFilterToggleGUI(style, null, null, node);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }
    }
}
