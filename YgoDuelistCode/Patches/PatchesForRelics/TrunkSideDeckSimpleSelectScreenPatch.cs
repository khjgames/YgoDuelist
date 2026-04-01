using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Adds trunk/side/split page nav buttons when <see cref="TrunkSideDeckGuiService"/> opens <see cref="NSimpleCardSelectScreen"/>.
/// The bar is parented as the <b>last</b> child so it draws above the card grid (Godot paints later siblings on top).
/// </summary>
[HarmonyPatch(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen.AfterOverlayOpened))]
public static class TrunkSideDeckSimpleSelectScreenPatch
{
    private const string NavRowName = "YgoTrunkSideNavRow";

    [HarmonyPostfix]
    public static void AfterOverlayOpened(NSimpleCardSelectScreen __instance)
    {
        if (!TrunkSideDeckGuiService.InjectNavButtonsOnNextGrid)
            return;
        if (__instance.GetNodeOrNull(NavRowName) != null)
            return;

        var row = new HBoxContainer
        {
            Name = NavRowName,
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        row.LayoutMode = 1; // anchors (matches other NSimpleCardSelectScreen add-ons)
        row.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        row.OffsetBottom = 56;
        row.AddThemeConstantOverride("separation", 8);

        TrunkSideDeckEditorPage page = TrunkSideDeckEditorSession.ActivePage;
        (string key1, TrunkSideDeckEditorPage target1, string key2, TrunkSideDeckEditorPage target2) = page switch
        {
            TrunkSideDeckEditorPage.Trunk => (
                "YGODUELIST-TRUNK_SIDE_DECK_RELIC.nav_edit_side_deck",
                TrunkSideDeckEditorPage.Side,
                "YGODUELIST-TRUNK_SIDE_DECK_RELIC.nav_split_editor",
                TrunkSideDeckEditorPage.Split),
            TrunkSideDeckEditorPage.Side => (
                "YGODUELIST-TRUNK_SIDE_DECK_RELIC.nav_edit_trunk",
                TrunkSideDeckEditorPage.Trunk,
                "YGODUELIST-TRUNK_SIDE_DECK_RELIC.nav_split_editor",
                TrunkSideDeckEditorPage.Split),
            _ => (
                "YGODUELIST-TRUNK_SIDE_DECK_RELIC.nav_edit_trunk",
                TrunkSideDeckEditorPage.Trunk,
                "YGODUELIST-TRUNK_SIDE_DECK_RELIC.nav_edit_side_deck",
                TrunkSideDeckEditorPage.Side)
        };

        row.AddChild(MakeNavButton(key1, target1));
        row.AddChild(MakeNavButton(key2, target2));

        __instance.AddChild(row);
    }

    private static Button MakeNavButton(string localizationKey, TrunkSideDeckEditorPage targetPage)
    {
        var button = new Button
        {
            Text = new LocString("relics", localizationKey).GetFormattedText()
        };
        button.Pressed += () => YgoRelicBrowseGridOverlayPatch.CompleteActiveTrunkSideNavigate(targetPage);
        return button;
    }
}
