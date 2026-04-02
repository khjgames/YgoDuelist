using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
/// Mod-only merchant branch: duplicated <see cref="NMerchantCard"/> slots and sell UI under vanilla <c>_slotsContainer</c>.
/// Does not replace vanilla fields, patch <see cref="NMerchantInventory.GetCardSlots"/>, or toggle vanilla row visibility—pages cover the slot area when active.
/// </summary>
public partial class YgoMerchantSlotsAddonLayer : Control
{
    public const string GodotName = "YgoMerchantSlotsAddonLayer";

    private static readonly MethodInfo UpdateNavigationMethod =
        AccessTools.DeclaredMethod(typeof(NMerchantInventory), "UpdateNavigation")!;

    private readonly NMerchantInventory _merchantUi;
    private readonly Player _player;
    private readonly Control _ygoBuyPage;
    private readonly GridContainer _ygoGrid;
    private readonly Control _ygoSellPage;
    private readonly VBoxContainer _sellList;
    private readonly Label _sellTally;
    private readonly Button _btnStandard;
    private readonly Button _btnYgoBuy;
    private readonly Button _btnYgoSell;

    private ShopPage _page = ShopPage.Standard;
    private YgoMerchantInventorySidecar? _sidecar;
    private readonly Dictionary<CardModel, CheckBox> _rowChecks = new();

    private enum ShopPage
    {
        Standard,
        YgoBuy,
        YgoSell
    }

    private YgoMerchantSlotsAddonLayer(
        NMerchantInventory merchantUi,
        Player player,
        Control ygoBuyPage,
        GridContainer ygoGrid,
        Control ygoSellPage,
        VBoxContainer sellList,
        Label sellTally,
        Button btnStandard,
        Button btnYgoBuy,
        Button btnYgoSell)
    {
        _merchantUi = merchantUi;
        _player = player;
        _ygoBuyPage = ygoBuyPage;
        _ygoGrid = ygoGrid;
        _ygoSellPage = ygoSellPage;
        _sellList = sellList;
        _sellTally = sellTally;
        _btnStandard = btnStandard;
        _btnYgoBuy = btnYgoBuy;
        _btnYgoSell = btnYgoSell;

        Name = GodotName;
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;

        TreeExiting += OnTreeExiting;

        _btnStandard.Pressed += () => ApplyPage(ShopPage.Standard);
        _btnYgoBuy.Pressed += () => ApplyPage(ShopPage.YgoBuy);
        _btnYgoSell.Pressed += () => ApplyPage(ShopPage.YgoSell);
    }

    /// <summary>
    /// Mount nav (on merchant root) and slot overlay (on slots container). Run from Harmony Prefix before vanilla <see cref="NMerchantInventory.Initialize"/> body fills cards.
    /// </summary>
    public static YgoMerchantSlotsAddonLayer? TryCreateAndMount(
        NMerchantInventory merchantUi,
        Player player,
        int ygoSlotCount,
        NMerchantCard templateCard)
    {
        var traverse = Traverse.Create(merchantUi);
        Control? slots = traverse.Field<Control>("_slotsContainer").Value;
        if (slots == null)
            return null;

        var ygoBuyPage = new Panel
        {
            Name = "YgoAddonBuyPage",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop
        };
        ygoBuyPage.SetAnchorsPreset(LayoutPreset.FullRect);
        ygoBuyPage.Modulate = new Color(0.06f, 0.06f, 0.08f, 0.94f);

        var ygoGrid = new GridContainer
        {
            Name = "YgoAddonBuyGrid",
            Columns = 4,
            MouseFilter = MouseFilterEnum.Stop
        };
        ygoGrid.SetAnchorsPreset(LayoutPreset.FullRect);
        const float pad = 20f;
        ygoGrid.OffsetLeft = pad;
        ygoGrid.OffsetTop = pad;
        ygoGrid.OffsetRight = -pad;
        ygoGrid.OffsetBottom = -pad;
        ygoGrid.AddThemeConstantOverride("h_separation", 18);
        ygoGrid.AddThemeConstantOverride("v_separation", 18);
        ygoBuyPage.AddChild(ygoGrid);

        for (int i = 0; i < ygoSlotCount; i++)
        {
            if (templateCard.Duplicate() is not NMerchantCard dup)
                continue;
            ygoGrid.AddChild(dup);
            dup.CustomMinimumSize = new Vector2(112, 198);
            dup.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill;
            dup.SizeFlagsVertical = SizeFlags.Expand | SizeFlags.Fill;
        }

        var ygoSellPage = new MarginContainer
        {
            Name = "YgoAddonSellPage",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop
        };
        ygoSellPage.SetAnchorsPreset(LayoutPreset.FullRect);
        ygoSellPage.AddThemeConstantOverride("margin_left", 6);
        ygoSellPage.AddThemeConstantOverride("margin_right", 6);
        var sellVBox = new VBoxContainer();
        ygoSellPage.AddChild(sellVBox);
        var sellTally = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 300),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill };
        scroll.AddChild(inner);
        var confirm = new Button { Text = "Review sell…" };
        sellVBox.AddChild(sellTally);
        sellVBox.AddChild(scroll);
        sellVBox.AddChild(confirm);

        var navBar = new HBoxContainer
        {
            Name = "YgoMerchantAddonNavBar",
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        var btnStandard = new Button { Text = "Standard shop" };
        var btnYgoBuy = new Button { Text = "YGO card offers", Visible = ygoSlotCount > 0 };
        var btnYgoSell = new Button { Text = "Sell (trunk / side)" };

        var layer = new YgoMerchantSlotsAddonLayer(
            merchantUi,
            player,
            ygoBuyPage,
            ygoGrid,
            ygoSellPage,
            inner,
            sellTally,
            btnStandard,
            btnYgoBuy,
            btnYgoSell);

        layer.AddChild(ygoBuyPage);
        layer.AddChild(ygoSellPage);

        slots.AddChild(layer);
        slots.MoveChild(layer, slots.GetChildCount() - 1);

        merchantUi.AddChild(navBar);
        merchantUi.MoveChild(navBar, merchantUi.GetChildCount() - 1);
        navBar.SetAnchorsPreset(LayoutPreset.TopLeft);
        Control? dialogue = merchantUi.GetNodeOrNull<Control>("%Dialogue");
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

        navBar.AddChild(btnStandard);
        navBar.AddChild(btnYgoBuy);
        navBar.AddChild(btnYgoSell);

        confirm.Pressed += layer.OnReviewSell;

        layer.ApplyPage(ShopPage.Standard);
        return layer;
    }

    public void CompleteAfterVanillaInitialize(MerchantInventory inventory)
    {
        if (!YgoMerchantInventorySidecarTable.TryGet(inventory, out YgoMerchantInventorySidecar? sidecar) || sidecar == null)
            return;

        _sidecar = sidecar;
        int n = _ygoGrid.GetChildCount();
        if (sidecar.YgoCardEntries.Count != n)
            return;

        for (int i = 0; i < n; i++)
        {
            if (_ygoGrid.GetChild(i) is not NMerchantCard card)
                continue;
            card.Initialize(_merchantUi);
            card.FillSlot(sidecar.YgoCardEntries[i]);
        }
    }

    private void ApplyPage(ShopPage page)
    {
        ShopPage prev = _page;
        bool enteringSell = page == ShopPage.YgoSell && prev != ShopPage.YgoSell;
        bool enteringYgoBuy = page == ShopPage.YgoBuy && prev != ShopPage.YgoBuy;
        _page = page;

        switch (page)
        {
            case ShopPage.Standard:
                Visible = false;
                _ygoBuyPage.Visible = false;
                _ygoSellPage.Visible = false;
                break;
            case ShopPage.YgoBuy:
                Visible = true;
                _ygoBuyPage.Visible = true;
                _ygoSellPage.Visible = false;
                if (enteringYgoBuy)
                {
                    foreach (Node ch in _ygoGrid.GetChildren())
                    {
                        if (ch is NMerchantCard ygoCard)
                            ygoCard.OnInventoryOpened();
                    }
                }

                break;
            case ShopPage.YgoSell:
                Visible = true;
                _ygoBuyPage.Visible = false;
                _ygoSellPage.Visible = true;
                if (enteringSell)
                    RebuildSellList();
                break;
        }

        UpdateNavigationMethod.Invoke(_merchantUi, null);
    }

    private void RebuildSellList()
    {
        foreach (Node ch in _sellList.GetChildren())
            ch.QueueFree();

        _rowChecks.Clear();

        foreach (CardModel card in YgoMerchantSellService.ListSellable(_player))
        {
            var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
            var cb = new CheckBox();
            var wrap = new Control { CustomMinimumSize = new Vector2(118, 172) };
            NCard? nc = NCard.Create(card);
            if (nc == null)
                continue;
            nc.Scale = new Vector2(0.4f, 0.4f);
            nc.Position = new Vector2(2, 2);
            wrap.AddChild(nc);
            row.AddChild(cb);
            row.AddChild(wrap);
            _sellList.AddChild(row);
            _rowChecks[card] = cb;
            cb.Toggled += _ => UpdateTally();
        }

        UpdateTally();
    }

    private void UpdateTally()
    {
        List<CardModel> picked = _rowChecks.Where(kv => kv.Value.ButtonPressed).Select(kv => kv.Key).ToList();
        YgoMerchantSellService.Tally(picked, out int c, out int u, out int r, out double ex, out int g);
        _sellTally.Text =
            $"Selected: {picked.Count} | commons×{c} uncommons×{u} rares×{r} | value {ex:0.##}g → pay {g}g";
    }

    private void OnReviewSell()
    {
        List<CardModel> picked = _rowChecks.Where(kv => kv.Value.ButtonPressed).Select(kv => kv.Key).ToList();
        if (picked.Count == 0)
            return;

        YgoMerchantSellService.Tally(picked, out int c, out int u, out int r, out double ex, out int g);
        string msg =
            $"Sell {c} commons, {u} uncommons, {r} rares for {g} gold?\n(Face value {ex:0.##}g; payout uses floor, remainder discarded.)";

        var d1 = new ConfirmationDialog { DialogText = msg, OkButtonText = "Continue" };
        _merchantUi.AddChild(d1);
        d1.PopupCentered();

        d1.Confirmed += () =>
        {
            d1.QueueFree();
            var d2 = new ConfirmationDialog
            {
                DialogText = "Final confirmation: remove these cards from trunk/side and gain the gold?",
                OkButtonText = "Sell"
            };
            _merchantUi.AddChild(d2);
            d2.PopupCentered();

            d2.Confirmed += () =>
            {
                d2.QueueFree();
                foreach (CardModel card in picked)
                    YgoMerchantSellService.RemoveFromTrunkOrSide(_player, card);
                TaskHelper.RunSafely(PlayerCmd.GainGold(g, _player));
                RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedGold(g);
                TrunkSideDeckRelic.NotifyRunTrunkSideChanged(_player);
                RebuildSellList();
                UpdateNavigationMethod.Invoke(_merchantUi, null);
            };

            d2.Canceled += () => d2.QueueFree();
        };

        d1.Canceled += () => d1.QueueFree();
    }

    private void OnTreeExiting()
    {
        if (_sidecar == null)
            return;
        foreach (MerchantCardEntry e in _sidecar.YgoCardEntries)
            e.PurchaseCompleted -= _sidecar.PurchaseUpdateHandler;
        _sidecar = null;
    }
}
