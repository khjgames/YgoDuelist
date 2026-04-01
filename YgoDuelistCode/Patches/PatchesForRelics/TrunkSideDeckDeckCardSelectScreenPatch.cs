using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Trunk/side/split editor uses <see cref="NDeckCardSelectScreen"/>; injects deck-view-style sort row (Obtained / Type / Cost / A–Z)
/// and two nav buttons aligned with the Type and Cost columns, slightly above that row. Chrome Y uses 0.99× deck view SortingOptions offset_top (92) so nav lines up like the vanilla sort bar.
/// </summary>
[HarmonyPatch(typeof(NDeckCardSelectScreen), nameof(NDeckCardSelectScreen._Ready))]
public static class TrunkSideDeckDeckCardSelectScreenPatch
{
    private const string ChromeName = "YgoTrunkSideDeckChrome";
    private const string SortButtonScenePath = "res://scenes/screens/deck_view_screen/deck_view_sort_button.tscn";

    private static readonly FieldInfo GridField =
        typeof(NCardGridSelectionScreen).GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo CardsField =
        typeof(NCardGridSelectionScreen).GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [HarmonyPostfix]
    public static void AfterReady(NDeckCardSelectScreen __instance)
    {
        if (!TrunkSideDeckGuiService.InjectNavButtonsOnNextGrid)
            return;
        if (__instance.GetNodeOrNull(ChromeName) != null)
            return;

        YgoRelicBrowseGridOverlayPatch.RegisterActiveTrunkSideDeckScreen(__instance);

        if (GridField.GetValue(__instance) is not NCardGrid grid)
            return;
        if (CardsField.GetValue(__instance) is not List<CardModel> cardList)
            return;

        var sortingPriority = new List<SortingOrders>
        {
            SortingOrders.Ascending,
            SortingOrders.TypeAscending,
            SortingOrders.CostAscending,
            SortingOrders.AlphabetAscending
        };

        PackedScene sortScene = ResourceLoader.Load<PackedScene>(SortButtonScenePath)!;

        const float sortButtonWidth = 250f;
        const float sortButtonHeight = 42f;
        // deck_view_screen.tscn: SortingOptions offset_top for the sort button strip (Type/Cost live here).
        const float deckViewSortingOptionsOffsetTop = 92f;
        float chromeOffsetTop = 0.99f * deckViewSortingOptionsOffsetTop;
        // Space between nav row and sort row: 99% of sort row height (nav sits just above Type/Cost, not a full 100px gap).
        float navGapPx = 0.99f * sortButtonHeight;
        const float chromeExtraMargin = 16f;
        float chromeHeight = sortButtonHeight + navGapPx + sortButtonHeight + chromeExtraMargin;

        var chrome = new Control
        {
            Name = ChromeName,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        chrome.LayoutMode = 1;
        chrome.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        chrome.OffsetTop = chromeOffsetTop;
        chrome.OffsetBottom = chromeOffsetTop + chromeHeight;

        var rootVBox = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        rootVBox.LayoutMode = 1;
        rootVBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        rootVBox.AddThemeConstantOverride("separation", 0);
        chrome.AddChild(rootVBox);

        var navRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        navRow.LayoutMode = 2;
        navRow.AddThemeConstantOverride("separation", 50);
        navRow.CustomMinimumSize = new Vector2(0, sortButtonHeight);

        var navPh0 = new Control { CustomMinimumSize = new Vector2(sortButtonWidth, sortButtonHeight) };
        var navPh3 = new Control { CustomMinimumSize = new Vector2(sortButtonWidth, sortButtonHeight) };
        navRow.AddChild(navPh0);

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

        NCardViewSortButton nav1 = MakeNavSortButton(sortScene, target1);
        NCardViewSortButton nav2 = MakeNavSortButton(sortScene, target2);
        navRow.AddChild(nav1);
        navRow.AddChild(nav2);
        navRow.AddChild(navPh3);

        var gap = new Control { CustomMinimumSize = new Vector2(0, navGapPx) };

        var sortRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        sortRow.LayoutMode = 2;
        sortRow.AddThemeConstantOverride("separation", 50);

        NCardViewSortButton MakeSorter()
        {
            var b = sortScene.Instantiate<NCardViewSortButton>(PackedScene.GenEditState.Disabled);
            b.CustomMinimumSize = new Vector2(sortButtonWidth, sortButtonHeight);
            b.LayoutMode = 2;
            return b;
        }

        NCardViewSortButton obtainedSorter = MakeSorter();
        NCardViewSortButton typeSorter = MakeSorter();
        NCardViewSortButton costSorter = MakeSorter();
        NCardViewSortButton alphabetSorter = MakeSorter();

        sortRow.AddChild(obtainedSorter);
        sortRow.AddChild(typeSorter);
        sortRow.AddChild(costSorter);
        sortRow.AddChild(alphabetSorter);

        var state = new ChromeState
        {
            Screen = __instance,
            Grid = grid,
            Cards = cardList,
            BaselineOrder = cardList.ToList(),
            SortingPriority = sortingPriority,
            ObtainedSorter = obtainedSorter,
            TypeSorter = typeSorter,
            CostSorter = costSorter,
            AlphabetSorter = alphabetSorter
        };

        rootVBox.AddChild(navRow);
        rootVBox.AddChild(gap);
        rootVBox.AddChild(sortRow);

        __instance.AddChild(chrome);
        __instance.MoveChild(chrome, __instance.GetChildCount() - 1);

        // NCardViewSortButton caches %Label in _Ready(); this postfix runs during NDeckCardSelectScreen._Ready,
        // so child _Ready (and _label) are not ready until after this frame — SetLabel must be deferred.
        Callable.From(() =>
        {
            nav1.SetLabel(new LocString("relics", key1).GetFormattedText());
            nav2.SetLabel(new LocString("relics", key2).GetFormattedText());
            HideSortGlyph(nav1);
            HideSortGlyph(nav2);
            obtainedSorter.SetLabel(new LocString("gameplay_ui", "SORT_OBTAINED").GetRawText());
            typeSorter.SetLabel(new LocString("gameplay_ui", "SORT_TYPE").GetRawText());
            costSorter.SetLabel(new LocString("gameplay_ui", "SORT_COST").GetRawText());
            alphabetSorter.SetLabel(new LocString("gameplay_ui", "SORT_ALPHABET").GetRawText());
        }).CallDeferred();

        if (__instance.GetNodeOrNull("%CardGrid") is Control cardGrid)
            cardGrid.OffsetTop = chromeOffsetTop + chromeHeight;

        state.ObtainedSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnObtainedSort(state)));
        state.TypeSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnCardTypeSort(state)));
        state.CostSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnCostSort(state)));
        state.AlphabetSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnAlphabetSort(state)));

        RefreshGridDisplay(state);
    }

    /// <summary>Same control as Type/Cost sort row (<see cref="NCardViewSortButton"/>); label/glyph after <see cref="NCardViewSortButton._Ready"/> via deferred apply.</summary>
    private static NCardViewSortButton MakeNavSortButton(PackedScene sortScene, TrunkSideDeckEditorPage targetPage)
    {
        NCardViewSortButton b = sortScene.Instantiate<NCardViewSortButton>(PackedScene.GenEditState.Disabled);
        b.CustomMinimumSize = new Vector2(250f, 42f);
        b.LayoutMode = 2;
        b.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => YgoRelicBrowseGridOverlayPatch.CompleteActiveTrunkSideNavigate(targetPage)));
        return b;
    }

    private static void HideSortGlyph(NCardViewSortButton b)
    {
        if (b.GetNodeOrNull("%Image") is TextureRect sortGlyph)
            sortGlyph.Visible = false;
    }

    private static void OnObtainedSort(ChromeState s)
    {
        s.SortingPriority.Remove(SortingOrders.Ascending);
        s.SortingPriority.Remove(SortingOrders.Descending);
        if (s.ObtainedSorter.IsDescending)
            s.SortingPriority.Insert(0, SortingOrders.Descending);
        else
            s.SortingPriority.Insert(0, SortingOrders.Ascending);
        RefreshGridDisplay(s);
    }

    private static void OnCardTypeSort(ChromeState s)
    {
        s.SortingPriority.Remove(SortingOrders.TypeAscending);
        s.SortingPriority.Remove(SortingOrders.TypeDescending);
        if (s.TypeSorter.IsDescending)
            s.SortingPriority.Insert(0, SortingOrders.TypeDescending);
        else
            s.SortingPriority.Insert(0, SortingOrders.TypeAscending);
        RefreshGridDisplay(s);
    }

    private static void OnCostSort(ChromeState s)
    {
        s.SortingPriority.Remove(SortingOrders.CostAscending);
        s.SortingPriority.Remove(SortingOrders.CostDescending);
        if (s.CostSorter.IsDescending)
            s.SortingPriority.Insert(0, SortingOrders.CostDescending);
        else
            s.SortingPriority.Insert(0, SortingOrders.CostAscending);
        RefreshGridDisplay(s);
    }

    private static void OnAlphabetSort(ChromeState s)
    {
        s.SortingPriority.Remove(SortingOrders.AlphabetAscending);
        s.SortingPriority.Remove(SortingOrders.AlphabetDescending);
        if (s.AlphabetSorter.IsDescending)
            s.SortingPriority.Insert(0, SortingOrders.AlphabetDescending);
        else
            s.SortingPriority.Insert(0, SortingOrders.AlphabetAscending);
        RefreshGridDisplay(s);
    }

    private static void RefreshGridDisplay(ChromeState s)
    {
        RebuildCardListOrder(s);
        s.Grid.YOffset = 100;
        s.Grid.SetCards(s.Cards, PileType.None, s.SortingPriority);
        CardsField.SetValue(s.Screen, s.Cards);
    }

    /// <summary>Mirrors <see cref="NCardGrid.SetCards"/> ordering so <see cref="NCardGridSelectionScreen"/> inspect uses correct indices.</summary>
    private static void RebuildCardListOrder(ChromeState s)
    {
        List<CardModel> next = s.BaselineOrder.ToList();
        SortingOrders p0 = s.SortingPriority[0];
        if (p0 == SortingOrders.Descending)
            next.Reverse();
        else if (p0 != SortingOrders.Ascending)
        {
            next.Sort((x, y) =>
            {
                foreach (SortingOrders item in s.SortingPriority)
                {
                    int num = CompareSortKey(x, y, item, s.BaselineOrder);
                    if (num != 0)
                        return num;
                }
                return x.Id.CompareTo(y.Id);
            });
        }

        s.Cards.Clear();
        s.Cards.AddRange(next);
    }

    private static int CompareSortKey(CardModel x, CardModel y, SortingOrders item, List<CardModel> baseline)
    {
        return item switch
        {
            SortingOrders.Ascending => baseline.IndexOf(x).CompareTo(baseline.IndexOf(y)),
            SortingOrders.Descending => -baseline.IndexOf(x).CompareTo(baseline.IndexOf(y)),
            SortingOrders.TypeAscending => x.Type.CompareTo(y.Type),
            SortingOrders.TypeDescending => -x.Type.CompareTo(y.Type),
            SortingOrders.CostAscending => x.EnergyCost.Canonical.CompareTo(y.EnergyCost.Canonical),
            SortingOrders.CostDescending => -x.EnergyCost.Canonical.CompareTo(y.EnergyCost.Canonical),
            SortingOrders.AlphabetAscending => string.Compare(x.Title, y.Title, LocManager.Instance.CultureInfo, CompareOptions.None),
            SortingOrders.AlphabetDescending => -string.Compare(x.Title, y.Title, LocManager.Instance.CultureInfo, CompareOptions.None),
            _ => 0
        };
    }

    private sealed class ChromeState
    {
        public required NDeckCardSelectScreen Screen { get; init; }
        public required NCardGrid Grid { get; init; }
        public required List<CardModel> Cards { get; init; }
        public required List<CardModel> BaselineOrder { get; init; }
        public required List<SortingOrders> SortingPriority { get; init; }
        public required NCardViewSortButton ObtainedSorter { get; init; }
        public required NCardViewSortButton TypeSorter { get; init; }
        public required NCardViewSortButton CostSorter { get; init; }
        public required NCardViewSortButton AlphabetSorter { get; init; }
    }
}
