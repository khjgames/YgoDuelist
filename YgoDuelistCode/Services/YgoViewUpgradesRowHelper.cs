using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Clones the deck-style "View Upgrades" row from <c>simple_cards_view_screen.tscn</c> and binds it to one or two <see cref="NCardGrid"/> instances.
/// </summary>
public static class YgoViewUpgradesRowHelper
{
    public const string RowNodeName = "YgoViewUpgradesRow";

    private static readonly string SimpleCardsViewScenePath =
        SceneHelper.GetScenePath("screens/simple_cards_view_screen");

    public static void Attach(Control host, NCardGrid primary, NCardGrid? secondary = null)
    {
        if (host.GetNodeOrNull(RowNodeName) != null)
            return;

        PackedScene? scene = PreloadManager.Cache.GetScene(SimpleCardsViewScenePath);
        if (scene == null)
            return;

        Control temp = scene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        Control? viewUpgrades = temp.GetNodeOrNull<Control>("ViewUpgrades");
        if (viewUpgrades == null)
        {
            temp.QueueFree();
            return;
        }

        // Do not Duplicate() this subtree: NTickbox._Ready resolves %TickboxVisuals by unique name; duplicated
        // nodes break that lookup and ConnectSignals null-ref. Use the instantiated node as-is.
        temp.RemoveChild(viewUpgrades);
        viewUpgrades.Name = RowNodeName;
        temp.QueueFree();

        host.AddChild(viewUpgrades);
        host.MoveChild(viewUpgrades, host.GetChildCount() - 1);

        NTickbox? tickbox = viewUpgrades.GetNodeOrNull<NTickbox>("MarginContainer/Upgrades");
        MegaLabel? label = viewUpgrades.GetNodeOrNull<MegaLabel>("MarginContainer/Upgrades/ViewUpgradesLabel");
        if (tickbox == null)
        {
            viewUpgrades.QueueFree();
            return;
        }

        tickbox.IsTicked = false;
        primary.IsShowingUpgrades = false;
        if (secondary != null)
            secondary.IsShowingUpgrades = false;

        label?.SetTextAutoSize(new LocString("gameplay_ui", "VIEW_UPGRADES").GetFormattedText());

        void Apply(NTickbox tb)
        {
            bool v = tb.IsTicked;
            primary.IsShowingUpgrades = v;
            if (secondary != null)
                secondary.IsShowingUpgrades = v;
        }

        tickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(Apply));

        void OnControllerUpdated()
        {
            if (NControllerManager.Instance == null)
                return;
            tickbox.Visible = !NControllerManager.Instance.IsUsingController;
            if (NControllerManager.Instance.IsUsingController)
            {
                tickbox.IsTicked = false;
                Apply(tickbox);
            }
        }

        OnControllerUpdated();
        if (NControllerManager.Instance != null)
        {
            NControllerManager.Instance.Connect(NControllerManager.SignalName.MouseDetected, Callable.From(OnControllerUpdated));
            NControllerManager.Instance.Connect(NControllerManager.SignalName.ControllerDetected, Callable.From(OnControllerUpdated));
        }

        if (NInputManager.Instance != null)
            NInputManager.Instance.Connect(NInputManager.SignalName.InputRebound, Callable.From(OnControllerUpdated));
    }

    /// <summary>Split trunk/side editor nests the right column grid under <c>YgoTrunkSideSplitHBox</c>.</summary>
    public static NCardGrid? FindSplitSideGrid(Control screen) =>
        screen.FindChild("YgoSplitSideGrid", recursive: true, owned: false) as NCardGrid;
}
