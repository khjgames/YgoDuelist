using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

internal static class YgoCardLibrarySidebarFilterRegistry
{
    internal static readonly ConditionalWeakTable<NCardLibrary, YgoCardLibrarySidebarFilterState> States = new();
}

internal static class YgoCardLibraryNCardLibraryInvoker
{
    static readonly MethodInfo UpdateFilterMethod =
        AccessTools.DeclaredMethod(typeof(NCardLibrary), "UpdateFilter", [typeof(bool)])
        ?? throw new InvalidOperationException("NCardLibrary.UpdateFilter(bool) not found.");

    static readonly MethodInfo DisplayCardsMethod =
        AccessTools.DeclaredMethod(typeof(NCardLibrary), "DisplayCards", Type.EmptyTypes)
        ?? throw new InvalidOperationException("NCardLibrary.DisplayCards not found.");

    internal static void RequestUpdateFilter(NCardLibrary library) =>
        UpdateFilterMethod.Invoke(library, [false]);

    internal static void RequestDisplayCards(NCardLibrary library)
    {
        var task = (Task)DisplayCardsMethod.Invoke(library, null)!;
        TaskHelper.RunSafely(task);
    }
}

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class YgoCardLibraryScrollAndPackTagsReadyPatch
{
    /// <summary>Compendium attribute row order (YGO strip: DIVINE first, then the six core attributes).</summary>
    static readonly DuelMonsterAttribute[] DuelMonsterAttributeFilterSidebarOrder =
    [
        DuelMonsterAttribute.Divine,
        DuelMonsterAttribute.Fire,
        DuelMonsterAttribute.Earth,
        DuelMonsterAttribute.Dark,
        DuelMonsterAttribute.Light,
        DuelMonsterAttribute.Wind,
        DuelMonsterAttribute.Water,
    ];

    static void Postfix(NCardLibrary __instance)
    {
        if (__instance.FindChild("YgoCardLibraryFilterScroll", recursive: true, owned: false) != null)
            return;

        var topVBox = __instance.GetNode<VBoxContainer>("Sidebar/MarginContainer/TopVBox");
        var cardTypeModule = topVBox.GetNode<Control>("CardTypeModule");
        var rarityModule = topVBox.GetNode<Control>("RarityModule");
        var costModule = topVBox.GetNode<Control>("CostModule");
        var alphabetSorter = __instance.GetNode<Control>("%AlphabetSorter");

        int insertIndex = cardTypeModule.GetIndex();

        topVBox.RemoveChild(alphabetSorter);
        topVBox.RemoveChild(costModule);
        topVBox.RemoveChild(rarityModule);
        topVBox.RemoveChild(cardTypeModule);

        var scroll = new ScrollContainer
        {
            Name = "YgoCardLibraryFilterScroll",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };

        var inner = new VBoxContainer
        {
            Name = "YgoCardLibraryFilterScrollInner",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        scroll.AddChild(inner);

        inner.AddChild(cardTypeModule);
        inner.AddChild(rarityModule);
        inner.AddChild(costModule);

        var filterState = new YgoCardLibrarySidebarFilterState();
        AppendYgoCardTypeCategory(inner, __instance, filterState);
        AppendLevelCategory(inner, __instance, filterState);
        AppendAtkCategory(inner, __instance, filterState);
        AppendDefCategory(inner, __instance, filterState);
        AppendAttributeCategory(inner, __instance, filterState);
        AppendRaceCategory(inner, __instance, filterState);
        AppendPackTagCategory(inner, __instance, filterState);
        YgoCardLibrarySidebarFilterRegistry.States.Add(__instance, filterState);

        inner.AddChild(alphabetSorter);

        var scrollWrap = new Control
        {
            Name = "YgoCardLibraryFilterScrollWrap",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        scrollWrap.AddChild(scroll);
        scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        scroll.OffsetBottom = -YgoCardLibraryLayoutTuning.FilterScrollBottomReservePixels;

        topVBox.AddChild(scrollWrap);
        topVBox.MoveChild(scrollWrap, insertIndex);

        // NTickbox / NCardTypeTickbox IsTicked touches visuals assigned in _Ready(); subtree only enters the tree above.
        filterState.ResetToDefaults();
        if (filterState.PackTags.SortButton != null)
            filterState.PackTags.SortButton.IsDescending = true;
        if (filterState.YgoCardTypes.SortButton != null)
            filterState.YgoCardTypes.SortButton.IsDescending = true;
        if (filterState.Level.SortButton != null)
            filterState.Level.SortButton.IsDescending = true;
        if (filterState.Attribute.SortButton != null)
            filterState.Attribute.SortButton.IsDescending = true;
        if (filterState.Race.SortButton != null)
            filterState.Race.SortButton.IsDescending = true;
        if (filterState.Atk.SortButton != null)
            filterState.Atk.SortButton.IsDescending = true;
        if (filterState.Def.SortButton != null)
            filterState.Def.SortButton.IsDescending = true;
    }

    static void AppendRaceCategory(
        VBoxContainer inner,
        NCardLibrary library,
        YgoCardLibrarySidebarFilterState root)
    {
        YgoCardLibraryRaceFilterState state = root.Race;
        var cat = new CardLibraryFilterSortingRuleCategoryGUI { Name = "YgoRaceModule" };
        cat.Setup("Race", () => YgoCardLibraryNCardLibraryInvoker.RequestDisplayCards(library));
        state.SortButton = cat.SortButton;
        inner.AddChild(cat);

        void Dirty() => YgoCardLibraryNCardLibraryInvoker.RequestUpdateFilter(library);

        CardLibraryFilterSortingRuleCategoryFilterToggleGUI AddToggle(string label, LocString hoverLoc)
        {
            var gui = CardLibraryFilterSortingRuleCategoryFilterToggleGUI.Create(
                CardLibraryFilterToggleStyle.Rarity,
                label,
                null,
                hoverLoc,
                triStateRarity: true);
            gui.ConnectChanged(Dirty);
            cat.ToggleColumn.AddChild(gui.Root);
            return gui;
        }

        foreach (DuelMonsterRace race in YgoCardLibraryRaceSidebarLabels.SidebarOrder)
        {
            string key = race.ToString();
            string label = YgoCardLibraryRaceSidebarLabels.TickboxLabel(race);
            var gui = AddToggle(label, new LocString("static_hover_tips", $"RACE_FILTER_{key}"));
            state.RaceToggles.Add((race, gui));
        }
    }

    static void AppendAttributeCategory(
        VBoxContainer inner,
        NCardLibrary library,
        YgoCardLibrarySidebarFilterState root)
    {
        YgoCardLibraryAttributeFilterState state = root.Attribute;
        var cat = new CardLibraryFilterSortingRuleCategoryGUI { Name = "YgoAttributeModule" };
        cat.Setup("Attribute", () => YgoCardLibraryNCardLibraryInvoker.RequestDisplayCards(library));
        state.SortButton = cat.SortButton;
        inner.AddChild(cat);

        void Dirty() => YgoCardLibraryNCardLibraryInvoker.RequestUpdateFilter(library);

        CardLibraryFilterSortingRuleCategoryFilterToggleGUI AddToggle(string label, LocString hoverLoc)
        {
            var gui = CardLibraryFilterSortingRuleCategoryFilterToggleGUI.Create(
                CardLibraryFilterToggleStyle.Rarity,
                label,
                null,
                hoverLoc);
            gui.ConnectChanged(Dirty);
            cat.ToggleColumn.AddChild(gui.Root);
            return gui;
        }

        foreach (DuelMonsterAttribute attr in DuelMonsterAttributeFilterSidebarOrder)
        {
            string key = attr.ToString();
            var gui = AddToggle(key, new LocString("static_hover_tips", $"ATTR_FILTER_{key}"));
            state.AttributeToggles.Add((attr, gui));
        }
    }

    static void AppendAtkCategory(
        VBoxContainer inner,
        NCardLibrary library,
        YgoCardLibrarySidebarFilterState root)
    {
        YgoCardLibraryStatRangeFilterState state = root.Atk;
        var cat = new CardLibraryFilterStatRangeSortingRuleCategoryGUI { Name = "YgoAtkModule" };
        cat.Setup(
            "ATK",
            new LocString("static_hover_tips", "STAT_FILTER_ATK_MIN"),
            new LocString("static_hover_tips", "STAT_FILTER_ATK_MAX"),
            () =>
            {
                root.PrimaryMonsterStatSort = YgoCardLibraryMonsterStatSortAxis.Atk;
                YgoCardLibraryNCardLibraryInvoker.RequestDisplayCards(library);
            });
        state.SortButton = cat.SortButton;
        state.MinEdit = cat.MinEdit;
        state.MaxEdit = cat.MaxEdit;
        inner.AddChild(cat);

        void Dirty() => YgoCardLibraryNCardLibraryInvoker.RequestUpdateFilter(library);
        cat.MinEdit.TextChanged += _ => Dirty();
        cat.MaxEdit.TextChanged += _ => Dirty();
    }

    static void AppendDefCategory(
        VBoxContainer inner,
        NCardLibrary library,
        YgoCardLibrarySidebarFilterState root)
    {
        YgoCardLibraryStatRangeFilterState state = root.Def;
        var cat = new CardLibraryFilterStatRangeSortingRuleCategoryGUI { Name = "YgoDefModule" };
        cat.Setup(
            "DEF",
            new LocString("static_hover_tips", "STAT_FILTER_DEF_MIN"),
            new LocString("static_hover_tips", "STAT_FILTER_DEF_MAX"),
            () =>
            {
                root.PrimaryMonsterStatSort = YgoCardLibraryMonsterStatSortAxis.Def;
                YgoCardLibraryNCardLibraryInvoker.RequestDisplayCards(library);
            });
        state.SortButton = cat.SortButton;
        state.MinEdit = cat.MinEdit;
        state.MaxEdit = cat.MaxEdit;
        inner.AddChild(cat);

        void Dirty() => YgoCardLibraryNCardLibraryInvoker.RequestUpdateFilter(library);
        cat.MinEdit.TextChanged += _ => Dirty();
        cat.MaxEdit.TextChanged += _ => Dirty();
    }

    static void AppendLevelCategory(
        VBoxContainer inner,
        NCardLibrary library,
        YgoCardLibrarySidebarFilterState root)
    {
        YgoCardLibraryLevelFilterState state = root.Level;
        var cat = new CardLibraryFilterSortingRuleCategoryGUI { Name = "YgoLevelModule" };
        cat.Setup("Level", () => YgoCardLibraryNCardLibraryInvoker.RequestDisplayCards(library));
        state.SortButton = cat.SortButton;
        inner.AddChild(cat);

        void Dirty() => YgoCardLibraryNCardLibraryInvoker.RequestUpdateFilter(library);

        var grid = new GridContainer
        {
            Name = "YgoLevelCostGrid",
            Columns = 4
        };
        grid.AddThemeConstantOverride("h_separation", 2);
        grid.AddThemeConstantOverride("v_separation", 2);
        cat.ToggleColumn.AddChild(grid);

        CardLibraryFilterSortingRuleCategoryFilterToggleGUI AddCostToggle(string label, LocString hoverLoc)
        {
            var gui = CardLibraryFilterSortingRuleCategoryFilterToggleGUI.Create(
                CardLibraryFilterToggleStyle.Cost,
                label,
                null,
                hoverLoc);
            gui.ConnectChanged(Dirty);
            grid.AddChild(gui.Root);
            return gui;
        }

        for (int lv = 1; lv <= 12; lv++)
        {
            state.LevelToggles[lv - 1] = AddCostToggle(
                lv.ToString(),
                new LocString("static_hover_tips", $"LEVEL_FILTER_{lv}"));
        }
    }

    static void AppendYgoCardTypeCategory(
        VBoxContainer inner,
        NCardLibrary library,
        YgoCardLibrarySidebarFilterState root)
    {
        YgoCardLibraryYgoCardTypeFilterState state = root.YgoCardTypes;
        var cat = new CardLibraryFilterSortingRuleCategoryGUI { Name = "YgoCardTypeModule" };
        cat.Setup("YgoCardType", () => YgoCardLibraryNCardLibraryInvoker.RequestDisplayCards(library));
        state.SortButton = cat.SortButton;
        inner.AddChild(cat);

        void Dirty() => YgoCardLibraryNCardLibraryInvoker.RequestUpdateFilter(library);

        var grid = new GridContainer
        {
            Name = "YgoCardTypeIconGrid",
            Columns = 4
        };
        grid.AddThemeConstantOverride("h_separation", 2);
        grid.AddThemeConstantOverride("v_separation", 2);
        cat.ToggleColumn.AddChild(grid);

        CardLibraryFilterSortingRuleCategoryFilterToggleGUI AddTypeToggle(Texture2D tex, LocString hoverLoc)
        {
            var gui = CardLibraryFilterSortingRuleCategoryFilterToggleGUI.Create(
                CardLibraryFilterToggleStyle.CardType,
                string.Empty,
                tex,
                hoverLoc);
            gui.ConnectChanged(Dirty);
            grid.AddChild(gui.Root);
            return gui;
        }

        state.NormalMonsterToggle = AddTypeToggle(
            YgoCardLibraryYgoCardTypeFilterIcons.NormalMonster.Value,
            new LocString("static_hover_tips", "YGO_TYPE_FILTER_NORMAL_MONSTER"));
        state.EffectMonsterToggle = AddTypeToggle(
            YgoCardLibraryYgoCardTypeFilterIcons.EffectMonster.Value,
            new LocString("static_hover_tips", "YGO_TYPE_FILTER_EFFECT_MONSTER"));
        state.TrapToggle = AddTypeToggle(
            YgoCardLibraryYgoCardTypeFilterIcons.Trap.Value,
            new LocString("static_hover_tips", "YGO_TYPE_FILTER_TRAP"));
        state.SpellToggle = AddTypeToggle(
            YgoCardLibraryYgoCardTypeFilterIcons.Spell.Value,
            new LocString("static_hover_tips", "YGO_TYPE_FILTER_SPELL"));
        state.FusionMonsterToggle = AddTypeToggle(
            YgoCardLibraryYgoCardTypeFilterIcons.FusionMonster.Value,
            new LocString("static_hover_tips", "YGO_TYPE_FILTER_FUSION_MONSTER"));
        state.RitualMonsterToggle = AddTypeToggle(
            YgoCardLibraryYgoCardTypeFilterIcons.RitualMonster.Value,
            new LocString("static_hover_tips", "YGO_TYPE_FILTER_RITUAL_MONSTER"));
    }

    static void AppendPackTagCategory(
        VBoxContainer inner,
        NCardLibrary library,
        YgoCardLibrarySidebarFilterState root)
    {
        YgoCardLibraryPackTagFilterState state = root.PackTags;
        var cat = new CardLibraryFilterSortingRuleCategoryGUI { Name = "YgoPackTagsModule" };
        cat.Setup("YGO Tags", () => YgoCardLibraryNCardLibraryInvoker.RequestDisplayCards(library));
        state.SortButton = cat.SortButton;
        inner.AddChild(cat);

        void Dirty() => YgoCardLibraryNCardLibraryInvoker.RequestUpdateFilter(library);

        CardLibraryFilterSortingRuleCategoryFilterToggleGUI AddToggle(string label, LocString hoverLoc)
        {
            var gui = CardLibraryFilterSortingRuleCategoryFilterToggleGUI.Create(
                CardLibraryFilterToggleStyle.Rarity,
                label,
                null,
                hoverLoc,
                triStateRarity: true);
            gui.ConnectChanged(Dirty);
            cat.ToggleColumn.AddChild(gui.Root);
            return gui;
        }

        state.NoneToggle = AddToggle("None", new LocString("static_hover_tips", "PACK_TAG_FILTER_NONE"));

        foreach (YgoCardPackTags tag in Enum.GetValues<YgoCardPackTags>())
        {
            if (tag == YgoCardPackTags.None)
                continue;
            string hoverKey = $"PACK_TAG_FILTER_{tag}";
            var gui = AddToggle(tag.ToString(), new LocString("static_hover_tips", hoverKey));
            state.FlagToggles.Add((tag, gui));
        }

        var spacer = new Control { CustomMinimumSize = new Vector2(0, 18) };
        inner.AddChild(spacer);
    }
}

[HarmonyPatch(typeof(NCardLibrary), "UpdateFilter", [typeof(bool)])]
public static class YgoCardLibraryCustomFilterPostPatch
{
    static readonly FieldInfo FilterField =
        AccessTools.Field(typeof(NCardLibrary), "_filter")
        ?? throw new InvalidOperationException("NCardLibrary._filter not found.");

    static void Postfix(NCardLibrary __instance)
    {
        if (!YgoCardLibrarySidebarFilterRegistry.States.TryGetValue(__instance, out YgoCardLibrarySidebarFilterState? state))
            return;

        var previous = (Func<CardModel, bool>)FilterField.GetValue(__instance)!;
        FilterField.SetValue(__instance, (Func<CardModel, bool>)(c => previous(c) && state.Matches(c)));
    }
}

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
public static class YgoCardLibraryCustomFilterSubmenuOpenedPatch
{
    static void Prefix(NCardLibrary __instance)
    {
        if (!YgoCardLibrarySidebarFilterRegistry.States.TryGetValue(__instance, out YgoCardLibrarySidebarFilterState? state))
            return;

        state.ResetToDefaults();
        if (state.PackTags.SortButton != null)
            state.PackTags.SortButton.IsDescending = true;
        if (state.YgoCardTypes.SortButton != null)
            state.YgoCardTypes.SortButton.IsDescending = true;
        if (state.Level.SortButton != null)
            state.Level.SortButton.IsDescending = true;
        if (state.Attribute.SortButton != null)
            state.Attribute.SortButton.IsDescending = true;
        if (state.Race.SortButton != null)
            state.Race.SortButton.IsDescending = true;
        if (state.Atk.SortButton != null)
            state.Atk.SortButton.IsDescending = true;
        if (state.Def.SortButton != null)
            state.Def.SortButton.IsDescending = true;
    }
}
