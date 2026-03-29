using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Adds trunk/side/split page nav buttons to the top of <see cref="NSimpleCardSelectScreen"/> when opened from <see cref="TrunkSideDeckGuiService"/>.
/// </summary>
[HarmonyPatch(typeof(NSimpleCardSelectScreen), "_Ready")]
public static class TrunkSideDeckSimpleSelectScreenPatch
{
    [HarmonyPostfix]
    public static void AfterReady(NSimpleCardSelectScreen __instance)
    {
        if (!TrunkSideDeckGuiService.InjectNavButtonsOnNextGrid)
            return;

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
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
        __instance.MoveChild(row, 0);
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
