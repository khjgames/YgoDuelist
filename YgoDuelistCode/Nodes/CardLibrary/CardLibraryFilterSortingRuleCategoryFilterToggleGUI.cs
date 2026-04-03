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
    private readonly YgoTriStateRarityTickController? _triRarity;

    private CardLibraryFilterSortingRuleCategoryFilterToggleGUI(
        CardLibraryFilterToggleStyle style,
        NCardTypeTickbox? type,
        NCardRarityTickbox? rarity,
        NCardCostTickbox? cost,
        YgoTriStateRarityTickController? triRarity)
    {
        _style = style;
        _type = type;
        _rarity = rarity;
        _cost = cost;
        _triRarity = triRarity;
    }

    public Control Root => _style switch
    {
        CardLibraryFilterToggleStyle.CardType => _type!,
        CardLibraryFilterToggleStyle.Rarity => _rarity!,
        CardLibraryFilterToggleStyle.Cost => _cost!,
        _ => throw new ArgumentOutOfRangeException()
    };

    /// <summary>Tri-state for rarity rows (pack tags, attribute, race). Two-state toggles map to neutral/include only.</summary>
    public CardLibraryFilterTriState RowState
    {
        get
        {
            if (_triRarity != null)
                return _triRarity.State;
            return IsTicked ? CardLibraryFilterTriState.Include : CardLibraryFilterTriState.Neutral;
        }
        set
        {
            if (_triRarity != null)
                _triRarity.SetState(value);
            else
                IsTicked = value == CardLibraryFilterTriState.Include;
        }
    }

    public bool IsTicked
    {
        get => _style switch
        {
            CardLibraryFilterToggleStyle.CardType => _type!.IsTicked,
            CardLibraryFilterToggleStyle.Rarity => _triRarity != null
                ? _triRarity.State == CardLibraryFilterTriState.Include
                : _rarity!.IsTicked,
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
                    if (_triRarity != null)
                        _triRarity.SetState(value ? CardLibraryFilterTriState.Include : CardLibraryFilterTriState.Neutral);
                    else
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
    /// <param name="triStateRarity">Rarity only: empty → check → ✕ → empty; drives <see cref="RowState"/>.</param>
    public static CardLibraryFilterSortingRuleCategoryFilterToggleGUI Create(
        CardLibraryFilterToggleStyle style,
        string labelText,
        Texture2D? iconTexture = null,
        LocString? hoverLoc = null,
        bool triStateRarity = false)
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
                return new CardLibraryFilterSortingRuleCategoryFilterToggleGUI(style, node, null, null, null);
            }
            case CardLibraryFilterToggleStyle.Rarity:
            {
                var path = SceneHelper.GetScenePath("screens/card_library/rarity_tickbox");
                var node = PreloadManager.Cache.GetScene(path).Instantiate<NCardRarityTickbox>();
                node.GetNode<MegaLabel>("Label").SetTextAutoSize(labelText);
                node.Loc = hoverLoc ?? PackTagRowHoverLoc;
                YgoTriStateRarityTickController? tri = null;
                if (triStateRarity)
                {
                    tri = new YgoTriStateRarityTickController(node);
                    YgoTriStateRarityTickRegistry.Register(node, tri);
                    Callable.From(() => tri.EnsureOverlay()).CallDeferred();
                }

                return new CardLibraryFilterSortingRuleCategoryFilterToggleGUI(style, null, node, null, tri);
            }
            case CardLibraryFilterToggleStyle.Cost:
            {
                var path = SceneHelper.GetScenePath("screens/card_library/card_cost_tickbox");
                var node = PreloadManager.Cache.GetScene(path).Instantiate<NCardCostTickbox>();
                var label = node.GetNode<MegaLabel>("%Label");
                label.SetTextAutoSize(labelText);
                node.Loc = hoverLoc ?? PackTagRowHoverLoc;
                return new CardLibraryFilterSortingRuleCategoryFilterToggleGUI(style, null, null, node, null);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }
    }
}
