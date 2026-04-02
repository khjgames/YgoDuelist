using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// 4×4 YGO buy grid (separate page), trunk/side sell page, and page nav next to the merchant area.
/// Standard merchant row (relics, potions, removal, colorless) stays vanilla; YGO offers are hidden until the YGO buy tab.
/// </summary>
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
public static class YgoMerchantShopUiPatch
{
    private const string ChromeMeta = "YgoMerchantChrome_v1";

    [HarmonyPrefix]
    public static void Prefix(NMerchantInventory __instance, MerchantInventory inventory, MerchantDialogueSet dialogue)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(inventory.Player))
            return;

        int need = inventory.CharacterCardEntries.Count;
        if (need < 1)
            return;

        if (__instance.HasMeta(ChromeMeta))
            return;

        YgoMerchantShopChrome.Prepare(__instance, inventory.Player, need);
        __instance.SetMeta(ChromeMeta, true);
    }
}

internal static class YgoMerchantShopChrome
{
    private static readonly ConditionalWeakTable<NMerchantInventory, MerchantUiState> States = new();

    private static readonly System.Reflection.MethodInfo UpdateNavigationMethod =
        AccessTools.DeclaredMethod(typeof(NMerchantInventory), "UpdateNavigation")!;

    private enum ShopPage
    {
        Standard,
        YgoBuy,
        YgoSell
    }

    private sealed class MerchantUiState
    {
        public required Control BuyGrid { get; init; }
        public required Control SellLayer { get; init; }
        public required Control? ColorlessCards { get; init; }
        public required Control? RelicContainer { get; init; }
        public required Control? PotionContainer { get; init; }
        public required Control? CardRemovalNode { get; init; }
        public required VBoxContainer SellList { get; init; }
        public required Label SellTally { get; init; }
        public required Player Player { get; init; }
        public required NMerchantInventory Inv { get; init; }
        public required Button BtnStandard { get; init; }
        public required Button BtnYgoBuy { get; init; }
        public required Button BtnYgoSell { get; init; }
        public ShopPage CurrentPage { get; set; }
        public readonly Dictionary<CardModel, CheckBox> RowChecks = new();
    }

    internal static void Prepare(NMerchantInventory inv, Player player, int slotCount)
    {
        var traverse = Traverse.Create(inv);
        Control? origChar = traverse.Field<Control>("_characterCardContainer").Value;
        Control? colorless = traverse.Field<Control>("_colorlessCardContainer").Value;
        Control? slots = traverse.Field<Control>("_slotsContainer").Value;
        Control? relics = traverse.Field<Control>("_relicContainer").Value;
        Control? potions = traverse.Field<Control>("_potionContainer").Value;
        Control? removal = traverse.Field<Control>("_cardRemovalNode").Value;
        if (origChar == null || slots == null || origChar.GetChildCount() < 1)
            return;

        if (origChar.GetChild(0) is not NMerchantCard template)
            return;

        var grid = new GridContainer
        {
            Name = "YgoMerchantBuyGrid",
            Columns = 4,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        slots.AddChild(grid);

        for (int i = 0; i < slotCount; i++)
        {
            if (template.Duplicate() is not NMerchantCard dup)
                continue;
            grid.AddChild(dup);
            dup.CustomMinimumSize = new Vector2(112, 198);
            dup.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
            dup.SizeFlagsVertical = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
        }

        origChar.Visible = false;

        traverse.Field<Control>("_characterCardContainer").Value = grid;

        MerchantUiState state = BuildShell(
            inv,
            player,
            grid,
            slots,
            colorless,
            relics,
            potions,
            removal);
        States.Add(inv, state);
    }

    private static MerchantUiState BuildShell(
        NMerchantInventory inv,
        Player player,
        Control buyGrid,
        Control slots,
        Control? colorless,
        Control? relicContainer,
        Control? potionContainer,
        Control? cardRemovalNode)
    {
        var navBar = new HBoxContainer
        {
            Name = "YgoMerchantPageNav",
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        inv.AddChild(navBar);
        inv.MoveChild(navBar, inv.GetChildCount() - 1);
        navBar.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        Control? dialogue = inv.GetNodeOrNull<Control>("%Dialogue");
        if (dialogue != null)
        {
            Vector2 d = dialogue.Position;
            navBar.Position = new Vector2(d.X - 560f, d.Y + 12f);
        }
        else
        {
            navBar.OffsetLeft = 48f;
            navBar.OffsetTop = 36f;
        }

        var btnStandard = new Button { Text = "Standard shop" };
        var btnYgoBuy = new Button { Text = "YGO card offers" };
        var btnYgoSell = new Button { Text = "Sell (trunk / side)" };
        navBar.AddChild(btnStandard);
        navBar.AddChild(btnYgoBuy);
        navBar.AddChild(btnYgoSell);

        var sellLayer = new MarginContainer { Visible = false };
        sellLayer.AddThemeConstantOverride("margin_left", 6);
        sellLayer.AddThemeConstantOverride("margin_right", 6);
        var sellVBox = new VBoxContainer();
        sellLayer.AddChild(sellVBox);

        var tally = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 300),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var inner = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(inner);

        var confirm = new Button { Text = "Review sell…" };
        sellVBox.AddChild(tally);
        sellVBox.AddChild(scroll);
        sellVBox.AddChild(confirm);

        slots.AddChild(sellLayer);
        slots.MoveChild(sellLayer, slots.GetChildCount() - 1);

        LayoutBuyGridFullShop(slots, buyGrid, sellLayer);

        var state = new MerchantUiState
        {
            BuyGrid = buyGrid,
            SellLayer = sellLayer,
            ColorlessCards = colorless,
            RelicContainer = relicContainer,
            PotionContainer = potionContainer,
            CardRemovalNode = cardRemovalNode,
            SellList = inner,
            SellTally = tally,
            Player = player,
            Inv = inv,
            BtnStandard = btnStandard,
            BtnYgoBuy = btnYgoBuy,
            BtnYgoSell = btnYgoSell,
            CurrentPage = ShopPage.Standard
        };

        btnStandard.Pressed += () => ApplyShopPage(state, ShopPage.Standard);
        btnYgoBuy.Pressed += () => ApplyShopPage(state, ShopPage.YgoBuy);
        btnYgoSell.Pressed += () => ApplyShopPage(state, ShopPage.YgoSell);

        confirm.Pressed += () => OnReviewSell(state);
        ApplyShopPage(state, ShopPage.Standard);
        return state;
    }

    /// <summary>
    /// Fill the rug <see cref="NMerchantInventory"/> slots area and draw above character/colorless/relic rows so this tab is a full-page layout.
    /// </summary>
    private static void LayoutBuyGridFullShop(Control slots, Control buyGrid, Control sellLayer)
    {
        buyGrid.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        const float pad = 20f;
        buyGrid.OffsetLeft = pad;
        buyGrid.OffsetTop = pad;
        buyGrid.OffsetRight = -pad;
        buyGrid.OffsetBottom = -pad;
        buyGrid.AddThemeConstantOverride("h_separation", 18);
        buyGrid.AddThemeConstantOverride("v_separation", 18);
        slots.MoveChild(buyGrid, sellLayer.GetIndex());
    }

    private static void ApplyShopPage(MerchantUiState s, ShopPage page)
    {
        bool enteringSell = page == ShopPage.YgoSell && s.CurrentPage != ShopPage.YgoSell;
        s.CurrentPage = page;

        switch (page)
        {
            case ShopPage.Standard:
                s.BuyGrid.Visible = false;
                s.SellLayer.Visible = false;
                if (s.ColorlessCards != null)
                    s.ColorlessCards.Visible = true;
                SetVanillaServiceRowsVisible(s, true);
                break;
            case ShopPage.YgoBuy:
                s.BuyGrid.Visible = true;
                s.SellLayer.Visible = false;
                if (s.ColorlessCards != null)
                    s.ColorlessCards.Visible = false;
                SetVanillaServiceRowsVisible(s, false);
                break;
            case ShopPage.YgoSell:
                s.BuyGrid.Visible = false;
                s.SellLayer.Visible = true;
                if (s.ColorlessCards != null)
                    s.ColorlessCards.Visible = false;
                SetVanillaServiceRowsVisible(s, false);
                if (enteringSell)
                    RebuildSellList(s);
                break;
        }

        RefreshNav(s.Inv);
    }

    private static void SetVanillaServiceRowsVisible(MerchantUiState s, bool visible)
    {
        if (s.RelicContainer != null)
            s.RelicContainer.Visible = visible;
        if (s.PotionContainer != null)
            s.PotionContainer.Visible = visible;
        if (s.CardRemovalNode != null)
            s.CardRemovalNode.Visible = visible;
    }

    private static void RefreshNav(NMerchantInventory inv) =>
        UpdateNavigationMethod.Invoke(inv, null);

    private static void RebuildSellList(MerchantUiState s)
    {
        foreach (Node ch in s.SellList.GetChildren())
            ch.QueueFree();

        s.RowChecks.Clear();

        foreach (CardModel card in YgoMerchantSellService.ListSellable(s.Player))
        {
            var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
            var cb = new CheckBox();
            var wrap = new Control { CustomMinimumSize = new Vector2(118, 172) };
            NCard nc = NCard.Create(card);
            nc.Scale = new Vector2(0.4f, 0.4f);
            nc.Position = new Vector2(2, 2);
            wrap.AddChild(nc);
            row.AddChild(cb);
            row.AddChild(wrap);
            s.SellList.AddChild(row);
            s.RowChecks[card] = cb;
            cb.Toggled += _ => UpdateTally(s);
        }

        UpdateTally(s);
    }

    private static void UpdateTally(MerchantUiState s)
    {
        List<CardModel> picked = s.RowChecks.Where(kv => kv.Value.ButtonPressed).Select(kv => kv.Key).ToList();
        YgoMerchantSellService.Tally(picked, out int c, out int u, out int r, out double ex, out int g);
        s.SellTally.Text =
            $"Selected: {picked.Count} | commons×{c} uncommons×{u} rares×{r} | value {ex:0.##}g → pay {g}g";
    }

    private static void OnReviewSell(MerchantUiState s)
    {
        List<CardModel> picked = s.RowChecks.Where(kv => kv.Value.ButtonPressed).Select(kv => kv.Key).ToList();
        if (picked.Count == 0)
            return;

        YgoMerchantSellService.Tally(picked, out int c, out int u, out int r, out double ex, out int g);
        string msg =
            $"Sell {c} commons, {u} uncommons, {r} rares for {g} gold?\n(Face value {ex:0.##}g; payout uses floor, remainder discarded.)";

        var d1 = new ConfirmationDialog { DialogText = msg, OkButtonText = "Continue" };
        s.Inv.AddChild(d1);
        d1.PopupCentered();

        d1.Confirmed += () =>
        {
            d1.QueueFree();
            var d2 = new ConfirmationDialog
            {
                DialogText = "Final confirmation: remove these cards from trunk/side and gain the gold?",
                OkButtonText = "Sell"
            };
            s.Inv.AddChild(d2);
            d2.PopupCentered();

            d2.Confirmed += () =>
            {
                d2.QueueFree();
                foreach (CardModel card in picked)
                    YgoMerchantSellService.RemoveFromTrunkOrSide(s.Player, card);
                TaskHelper.RunSafely(PlayerCmd.GainGold(g, s.Player));
                RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedGold(g);
                TrunkSideDeckRelic.NotifyRunTrunkSideChanged(s.Player);
                RebuildSellList(s);
                RefreshNav(s.Inv);
            };

            d2.Canceled += () => d2.QueueFree();
        };

        d1.Canceled += () => d1.QueueFree();
    }
}
