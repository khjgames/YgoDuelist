using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>
/// Card-library sidebar section: sort header plus min/max numeric <see cref="LineEdit"/> row (ATK/DEF, etc.).
/// </summary>
public partial class CardLibraryFilterStatRangeSortingRuleCategoryGUI : VBoxContainer
{
    private NCardViewSortButton? _sortButton;
    private LineEdit? _minEdit;
    private LineEdit? _maxEdit;

    public NCardViewSortButton SortButton => _sortButton!;

    public LineEdit MinEdit => _minEdit!;

    public LineEdit MaxEdit => _maxEdit!;

    public void Setup(
        string sortButtonLabel,
        LocString minHoverLoc,
        LocString maxHoverLoc,
        System.Action onSortReleased)
    {
        AddThemeConstantOverride("separation", 4);
        _sortButton = PreloadManager.Cache
            .GetScene(SceneHelper.GetScenePath("screens/card_library/library_sort_button"))
            .Instantiate<NCardViewSortButton>();
        AddChild(_sortButton);
        _sortButton.GetNode<MegaLabel>("%Label").SetTextAutoSize(sortButtonLabel);
        _sortButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => onSortReleased()));

        var row = new HBoxContainer { Name = "StatMinMaxRow" };
        row.AddThemeConstantOverride("separation", 6);
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(row);

        var minLabel = new MegaLabel();
        minLabel.SetTextAutoSize("Min");
        minLabel.CustomMinimumSize = new Vector2(28, 0);
        row.AddChild(minLabel);

        _minEdit = new LineEdit { Name = "MinStatEdit", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _minEdit.MaxLength = 6;
        _minEdit.TooltipText = minHoverLoc.GetFormattedText();
        row.AddChild(_minEdit);

        var maxLabel = new MegaLabel();
        maxLabel.SetTextAutoSize("Max");
        maxLabel.CustomMinimumSize = new Vector2(32, 0);
        row.AddChild(maxLabel);

        _maxEdit = new LineEdit { Name = "MaxStatEdit", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _maxEdit.MaxLength = 6;
        _maxEdit.TooltipText = maxHoverLoc.GetFormattedText();
        row.AddChild(_maxEdit);
    }
}
