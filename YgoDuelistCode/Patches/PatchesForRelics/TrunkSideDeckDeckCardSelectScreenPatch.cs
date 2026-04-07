using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Trunk/side/split editor uses <see cref="NDeckCardSelectScreen"/>; injects deck-view-style sort row (Obtained / Type / Cost / A–Z)
/// with two nav buttons below it. Split page replaces the scene <see cref="NCardGrid"/> with two fresh instances (same source as the right column)
/// so neither grid runs <see cref="NCardGrid.SetCards"/> while full-width; vanilla deferred scroll sizing otherwise pins the trunk column to full width.
/// Enables <see cref="Control.ClipContents"/> on the editor grids so cards are clipped at the grid bounds (no overdraw past the scroll viewport).
/// </summary>
[HarmonyPatch(typeof(NDeckCardSelectScreen), nameof(NDeckCardSelectScreen._Ready))]
public static class TrunkSideDeckDeckCardSelectScreenPatch
{
    private static ChromeState? ActiveEditorChrome;

    internal static void ClearActiveEditorChrome(NDeckCardSelectScreen? screen)
    {
        if (screen != null && ActiveEditorChrome != null && ReferenceEquals(ActiveEditorChrome.Screen, screen))
            ActiveEditorChrome = null;
    }

    /// <summary>After pile mutation from sticky Confirm, refresh baselines and grids for the active trunk/side chrome.</summary>
    internal static void ResyncChromeAfterPileMutation()
    {
        ChromeState? state = ActiveEditorChrome;
        if (state == null)
            return;
        Player? p = TrunkSideDeckGuiService.EditorSessionPlayer;
        if (p == null)
            return;

        CardPile trunk = PlayerRunTrunk.GetOrCreatePile(p);
        CardPile side = PlayerRunSideDeck.GetOrCreatePile(p);
        TrunkSideDeckEditorPage page = TrunkSideDeckEditorSession.ActivePage;

        if (state.RightGrid != null && state.TrunkBaseline != null && state.SideBaseline != null)
        {
            state.TrunkBaseline.Clear();
            state.TrunkBaseline.AddRange(trunk.Cards);
            state.SideBaseline.Clear();
            state.SideBaseline.AddRange(side.Cards);
            TrunkSideDeckGuiService.SetSplitSessionPiles(new List<CardModel>(state.TrunkBaseline), new List<CardModel>(state.SideBaseline));
        }
        else
        {
            List<CardModel> nextBaseline = page switch
            {
                TrunkSideDeckEditorPage.Trunk => new List<CardModel>(trunk.Cards),
                TrunkSideDeckEditorPage.Side => new List<CardModel>(side.Cards),
                _ => trunk.Cards.Concat(side.Cards).ToList()
            };
            state.BaselineOrder.Clear();
            state.BaselineOrder.AddRange(nextBaseline);
        }

        RefreshGridDisplay(state);
    }

    private const string ChromeName = "YgoTrunkSideDeckChrome";
    private const string SplitHBoxName = "YgoTrunkSideSplitHBox";
    private const string SortButtonScenePath = "res://scenes/screens/deck_view_screen/deck_view_sort_button.tscn";

    /// <summary>Same scene as <see cref="NDeckCardSelectScreen"/>; used to clone an empty <see cref="NCardGrid"/> (live grid after <see cref="NCardGrid.SetCards"/> cannot be <see cref="Godot.Node.Duplicate"/>d).</summary>
    private static readonly string DeckCardSelectScenePath =
        SceneHelper.GetScenePath("screens/card_selection/deck_card_select_screen");

    /// <summary>
    /// <see cref="NCardGrid"/> uses <c>Columns = (scrollWidth + 40) / (cardWidth + 40)</c>. If width is 0 before layout, <c>Columns</c> is 0,
    /// <see cref="NCardGrid"/> builds empty rows, and <c>AllocateCardHolders</c> indexes <c>_cardRows[0][0]</c> and throws.
    /// </summary>
    private static float SplitGridSlotMinWidth => NCard.defaultSize.X * NCardHolder.smallScale.X + 40f;

    private static readonly FieldInfo GridField =
        typeof(NCardGridSelectionScreen).GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo CardsField =
        typeof(NCardGridSelectionScreen).GetField("_cards", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo? DeckOnCardClicked =
        AccessTools.DeclaredMethod(typeof(NDeckCardSelectScreen), "OnCardClicked", new[] { typeof(CardModel) });

    private static readonly MethodInfo? GridScreenShowCardDetail =
        AccessTools.DeclaredMethod(typeof(NCardGridSelectionScreen), "ShowCardDetail", new[] { typeof(CardModel) });

    [HarmonyPostfix]
    public static void AfterReady(NDeckCardSelectScreen __instance)
    {
        if (!TrunkSideDeckGuiService.InjectNavButtonsOnNextGrid)
            return;
        if (__instance.GetNodeOrNull(ChromeName) != null)
            return;

        // Vanilla %PeekButton (eye) hides overlay chrome; not wanted for trunk/side editor (NPeekButton.Disable() clears visibility).
        __instance.GetNodeOrNull<NPeekButton>("%PeekButton")?.Disable();

        YgoRelicBrowseGridOverlayPatch.RegisterActiveTrunkSideDeckScreen(__instance);

        if (GridField.GetValue(__instance) is not NCardGrid grid)
            return;
        if (CardsField.GetValue(__instance) is not List<CardModel> cardList)
            return;

        TrunkSideDeckSplitGridState.Clear();
        NCardGrid? rightSplitGrid = null;
        List<CardModel>? trunkBaseline = null;
        List<CardModel>? sideBaseline = null;
        bool splitMode = TryBeginSplitDualLayout(__instance, ref grid, out rightSplitGrid, out trunkBaseline, out sideBaseline);
        if (splitMode && rightSplitGrid != null)
            TrunkSideDeckSplitGridState.Activate(grid, rightSplitGrid);

        // Clip card drawing to the grid rect (vanilla grid scrolls a Control, not ScrollContainer — without this, cards can overdraw past the viewport).
        // Split mode also sets clip on grids + column slots inside TryBeginSplitDualLayout.
        grid.ClipContents = true;
        if (rightSplitGrid != null)
            rightSplitGrid.ClipContents = true;

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
        const float deckViewSortingOptionsOffsetTop = 92f;
        float chromeOffsetTop = 0.99f * deckViewSortingOptionsOffsetTop;
        float navGapPx = 1.5f * sortButtonHeight;
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
        var gap2 = new Control { CustomMinimumSize = new Vector2(0, 12f) };

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

        ChromeState state = splitMode && trunkBaseline != null && sideBaseline != null && rightSplitGrid != null
            ? new ChromeState
            {
                Screen = __instance,
                Grid = grid,
                RightGrid = rightSplitGrid,
                Cards = cardList,
                BaselineOrder = cardList.ToList(),
                TrunkBaseline = trunkBaseline,
                SideBaseline = sideBaseline,
                SortingPriority = sortingPriority,
                ObtainedSorter = obtainedSorter,
                TypeSorter = typeSorter,
                CostSorter = costSorter,
                AlphabetSorter = alphabetSorter
            }
            : new ChromeState
            {
                Screen = __instance,
                Grid = grid,
                RightGrid = null,
                Cards = cardList,
                BaselineOrder = cardList.ToList(),
                TrunkBaseline = null,
                SideBaseline = null,
                SortingPriority = sortingPriority,
                ObtainedSorter = obtainedSorter,
                TypeSorter = typeSorter,
                CostSorter = costSorter,
                AlphabetSorter = alphabetSorter
            };

        // Sort filters on top; Trunk / Side / Split nav row below (same total chrome height).
        rootVBox.AddChild(gap);
        rootVBox.AddChild(sortRow);
        rootVBox.AddChild(gap2);
        rootVBox.AddChild(navRow);

        __instance.AddChild(chrome);
        __instance.MoveChild(chrome, __instance.GetChildCount() - 1);

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

        Control? gridTop = splitMode
            ? __instance.GetNodeOrNull(SplitHBoxName) as Control
            : __instance.GetNodeOrNull("%CardGrid") as Control;
        if (gridTop != null)
        {
            gridTop.OffsetTop = chromeOffsetTop + chromeHeight;
            if (splitMode)
            {
                // Only changing OffsetTop would stretch the split hbox vs the single %CardGrid (clip area becomes too tall).
                // Nudge top and bottom in opposite directions by the same amount to shift the block without changing height.
                float shift = TrunkSideDeckGuiService.SplitEditorHBoxOffsetTopAdjust;
                gridTop.OffsetTop += shift;
                gridTop.OffsetBottom -= shift;
                // Optional: extra bottom inset only (positive usually shortens the split viewport — tune for your theme).
                gridTop.OffsetBottom += TrunkSideDeckGuiService.SplitEditorHBoxOffsetBottomAdjust;
            }
        }

        state.ObtainedSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnObtainedSort(state)));
        state.TypeSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnCardTypeSort(state)));
        state.CostSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnCostSort(state)));
        state.AlphabetSorter.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => OnAlphabetSort(state)));

        if (splitMode && rightSplitGrid != null)
            Callable.From(() => RefreshGridDisplay(state)).CallDeferred();
        else
            RefreshGridDisplay(state);

        ActiveEditorChrome = state;
    }

    private static bool TryBeginSplitDualLayout(
        NDeckCardSelectScreen screen,
        ref NCardGrid grid,
        out NCardGrid? rightGrid,
        out List<CardModel>? trunkBaseline,
        out List<CardModel>? sideBaseline)
    {
        rightGrid = null;
        trunkBaseline = null;
        sideBaseline = null;

        if (TrunkSideDeckEditorSession.ActivePage != TrunkSideDeckEditorPage.Split)
            return false;
        if (TrunkSideDeckGuiService.SplitSessionTrunkOrder == null || TrunkSideDeckGuiService.SplitSessionSideOrder == null)
            return false;

        trunkBaseline = new List<CardModel>(TrunkSideDeckGuiService.SplitSessionTrunkOrder);
        sideBaseline = new List<CardModel>(TrunkSideDeckGuiService.SplitSessionSideOrder);

        NCardGrid? freshLeft = InstantiateFreshCardGridFromDeckSelectScene();
        NCardGrid? rg = InstantiateFreshCardGridFromDeckSelectScene();
        if (freshLeft == null || rg == null)
        {
            freshLeft?.QueueFree();
            rg?.QueueFree();
            return false;
        }

        NCardGrid originalGrid = grid;
        Node parent = originalGrid.GetParent()!;
        int insertIndex = originalGrid.GetIndex();
        CopyAnchoredControlLayout(originalGrid, out float al, out float at, out float ar, out float ab, out float ol, out float ot, out float orr, out float ob);
        parent.RemoveChild(originalGrid);
        originalGrid.QueueFree();

        var hbox = new HBoxContainer { Name = SplitHBoxName };
        hbox.LayoutMode = 1;
        hbox.AnchorLeft = al;
        hbox.AnchorTop = at;
        hbox.AnchorRight = ar;
        hbox.AnchorBottom = ab;
        hbox.OffsetLeft = ol;
        hbox.OffsetTop = ot;
        hbox.OffsetRight = orr;
        hbox.OffsetBottom = ob;

        parent.AddChild(hbox);
        parent.MoveChild(hbox, insertIndex);

        var leftSlot = new Control();
        leftSlot.LayoutMode = 2;
        leftSlot.CustomMinimumSize = new Vector2(SplitGridSlotMinWidth, 0f);
        leftSlot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftSlot.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        var rightSlot = new Control();
        rightSlot.LayoutMode = 2;
        rightSlot.CustomMinimumSize = new Vector2(SplitGridSlotMinWidth, 0f);
        rightSlot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rightSlot.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        hbox.AddChild(leftSlot);
        hbox.AddChild(rightSlot);

        freshLeft.Name = "CardGrid";
        freshLeft.UniqueNameInOwner = true;
        leftSlot.AddChild(freshLeft);
        freshLeft.LayoutMode = 1;
        freshLeft.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        freshLeft.OffsetLeft = freshLeft.OffsetRight = freshLeft.OffsetTop = freshLeft.OffsetBottom = 0;

        rg.Name = "YgoSplitSideGrid";
        rightSlot.AddChild(rg);
        rg.LayoutMode = 1;
        rg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        rg.OffsetLeft = rg.OffsetRight = rg.OffsetTop = rg.OffsetBottom = 0;

        // Split columns: clip at grid and slot so card scroll content cannot overdraw past the column bounds.
        freshLeft.ClipContents = true;
        rg.ClipContents = true;
        leftSlot.ClipContents = true;
        rightSlot.ClipContents = true;

        grid = freshLeft;
        GridField.SetValue(screen, freshLeft);

        rightGrid = rg;
        NDeckCardSelectScreen screenRef = screen;
        void ConnectSplitGridSignals(NCardGrid g)
        {
            g.Connect(
                NCardGrid.SignalName.HolderPressed,
                Callable.From<NCardHolder>(h => DeckOnCardClicked?.Invoke(screenRef, new object[] { h.CardModel })));
            g.Connect(
                NCardGrid.SignalName.HolderAltPressed,
                Callable.From<NCardHolder>(h => GridScreenShowCardDetail?.Invoke(screenRef, new object[] { h.CardModel })));
        }

        ConnectSplitGridSignals(freshLeft);
        ConnectSplitGridSignals(rg);

        Callable.From(() =>
        {
            freshLeft.InsetForTopBar();
            rg.InsetForTopBar();
        }).CallDeferred();

        return true;
    }

    private static NCardGrid? InstantiateFreshCardGridFromDeckSelectScene()
    {
        PackedScene ps = PreloadManager.Cache.GetScene(DeckCardSelectScenePath);
        NDeckCardSelectScreen temp = ps.Instantiate<NDeckCardSelectScreen>(PackedScene.GenEditState.Disabled);
        try
        {
            NCardGrid template = temp.GetNode<NCardGrid>("%CardGrid");
            Node dup = template.Duplicate();
            return dup as NCardGrid;
        }
        finally
        {
            temp.QueueFree();
        }
    }

    private static void CopyAnchoredControlLayout(
        Control src,
        out float anchorLeft,
        out float anchorTop,
        out float anchorRight,
        out float anchorBottom,
        out float offsetLeft,
        out float offsetTop,
        out float offsetRight,
        out float offsetBottom)
    {
        anchorLeft = src.AnchorLeft;
        anchorTop = src.AnchorTop;
        anchorRight = src.AnchorRight;
        anchorBottom = src.AnchorBottom;
        offsetLeft = src.OffsetLeft;
        offsetTop = src.OffsetTop;
        offsetRight = src.OffsetRight;
        offsetBottom = src.OffsetBottom;
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
        if (s.RightGrid != null && s.TrunkBaseline != null && s.SideBaseline != null)
        {
            List<CardModel> trunkOrdered = SortSegment(s.TrunkBaseline, s.SortingPriority);
            List<CardModel> sideOrdered = SortSegment(s.SideBaseline, s.SortingPriority);
            s.Cards.Clear();
            s.Cards.AddRange(trunkOrdered);
            s.Cards.AddRange(sideOrdered);

            int splitY = TrunkSideDeckGuiService.SplitEditorNCardGridContentYOffset;
            s.Grid.YOffset = splitY;
            s.RightGrid.YOffset = splitY;
            s.Grid.SetCards(trunkOrdered, PileType.None, s.SortingPriority);
            s.RightGrid.SetCards(sideOrdered, PileType.None, s.SortingPriority);
            CardsField.SetValue(s.Screen, s.Cards);
            return;
        }

        RebuildCombinedCardListOrder(s);
        s.Grid.YOffset = 100;
        s.Grid.SetCards(s.Cards, PileType.None, s.SortingPriority);
        CardsField.SetValue(s.Screen, s.Cards);
    }

    private static void RebuildCombinedCardListOrder(ChromeState s)
    {
        List<CardModel> next = SortSegment(s.BaselineOrder, s.SortingPriority);
        s.Cards.Clear();
        s.Cards.AddRange(next);
    }

    private static List<CardModel> SortSegment(List<CardModel> baseline, List<SortingOrders> sortingPriority)
    {
        List<CardModel> next = new List<CardModel>(baseline);
        SortingOrders p0 = sortingPriority[0];
        if (p0 == SortingOrders.Descending)
            next.Reverse();
        else if (p0 != SortingOrders.Ascending)
        {
            next.Sort((x, y) =>
            {
                foreach (SortingOrders item in sortingPriority)
                {
                    int num = CompareSortKey(x, y, item, baseline);
                    if (num != 0)
                        return num;
                }

                return x.Id.CompareTo(y.Id);
            });
        }

        return next;
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
        public NCardGrid? RightGrid { get; init; }
        public required List<CardModel> Cards { get; init; }
        public required List<CardModel> BaselineOrder { get; init; }
        public List<CardModel>? TrunkBaseline { get; init; }
        public List<CardModel>? SideBaseline { get; init; }
        public required List<SortingOrders> SortingPriority { get; init; }
        public required NCardViewSortButton ObtainedSorter { get; init; }
        public required NCardViewSortButton TypeSorter { get; init; }
        public required NCardViewSortButton CostSorter { get; init; }
        public required NCardViewSortButton AlphabetSorter { get; init; }
    }
}
