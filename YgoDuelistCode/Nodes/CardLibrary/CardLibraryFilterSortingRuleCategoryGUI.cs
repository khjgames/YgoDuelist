using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>
/// One card-library sidebar section: vanilla-style sort header plus a column for filter toggles.
/// Right-click the header or any toggle row to collapse or expand all filter options in this section.
/// </summary>
public partial class CardLibraryFilterSortingRuleCategoryGUI : VBoxContainer
{
    static readonly StringName MetaRmbExpandWired = "YgoCategoryRmbExpandWired";

    private NCardViewSortButton? _sortButton;
    private VBoxContainer? _toggleColumn;

    public NCardViewSortButton SortButton => _sortButton!;

    public VBoxContainer ToggleColumn => _toggleColumn!;

    public void Setup(string sortButtonLabel, System.Action onSortReleased)
    {
        AddThemeConstantOverride("separation", 4);
        _sortButton = PreloadManager.Cache
            .GetScene(SceneHelper.GetScenePath("screens/card_library/library_sort_button"))
            .Instantiate<NCardViewSortButton>();
        AddChild(_sortButton);
        // NCardViewSortButton.SetLabel touches _label from _Ready(); subtree may not be in the scene tree yet.
        _sortButton.GetNode<MegaLabel>("%Label").SetTextAutoSize(sortButtonLabel);
        _sortButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => onSortReleased()));
        _sortButton.GuiInput += e => OnRightClickToggleToggleColumn(_sortButton, e);

        _toggleColumn = new VBoxContainer { Name = "CategoryToggles" };
        _toggleColumn.AddThemeConstantOverride("separation", 0);
        AddChild(_toggleColumn);
        WireRightClickExpandCollapseRecursive(_toggleColumn);
    }

    void OnToggleSubtreeChildEntered(Node node)
    {
        if (node is Control ch)
            WireRightClickExpandCollapseRecursive(ch);
    }

    void WireRightClickExpandCollapseRecursive(Control c)
    {
        if (c.HasMeta(MetaRmbExpandWired))
            return;
        c.SetMeta(MetaRmbExpandWired, true);
        c.GuiInput += e => OnRightClickToggleToggleColumn(c, e);
        c.ChildEnteredTree += OnToggleSubtreeChildEntered;
        foreach (Node child in c.GetChildren())
        {
            if (child is Control ch)
                WireRightClickExpandCollapseRecursive(ch);
        }
    }

    void OnRightClickToggleToggleColumn(Control source, InputEvent e)
    {
        if (e is not InputEventMouseButton mb || !mb.Pressed || mb.ButtonIndex != MouseButton.Right)
            return;
        if (_toggleColumn == null)
            return;
        _toggleColumn.Visible = !_toggleColumn.Visible;
        source.AcceptEvent();
    }
}
