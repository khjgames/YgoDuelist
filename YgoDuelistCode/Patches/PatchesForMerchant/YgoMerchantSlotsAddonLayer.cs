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
/// YGO buy/sell pages hide vanilla card/relic/potion/removal rows and dialogue chrome so only the rug texture, hand, and mod UI show; Standard restores them.
/// </summary>
public partial class YgoMerchantSlotsAddonLayer : Control
{
    public const string GodotName = "YgoMerchantSlotsAddonLayer";

    /// <summary>Root <see cref="HBoxContainer"/> parented to <see cref="NMerchantInventory"/> (Standard / YGO buy / sell tabs).</summary>
    public const string AddonNavBarName = "YgoMerchantAddonNavBar";

    private const string BuyGridCenterName = "YgoAddonBuyGridCenter";

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

    private readonly Control? _vanillaCharacterCards;
    private readonly Control? _vanillaColorlessCards;
    private readonly Control? _vanillaRelics;
    private readonly Control? _vanillaPotions;
    private readonly NMerchantCardRemoval? _vanillaCardRemoval;
    private readonly Control? _merchantDialogueRoot;

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
        Button btnYgoSell,
        Control? vanillaCharacterCards,
        Control? vanillaColorlessCards,
        Control? vanillaRelics,
        Control? vanillaPotions,
        NMerchantCardRemoval? vanillaCardRemoval,
        Control? merchantDialogueRoot)
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
        _vanillaCharacterCards = vanillaCharacterCards;
        _vanillaColorlessCards = vanillaColorlessCards;
        _vanillaRelics = vanillaRelics;
        _vanillaPotions = vanillaPotions;
        _vanillaCardRemoval = vanillaCardRemoval;
        _merchantDialogueRoot = merchantDialogueRoot;

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
        Control? slots = traverse.Field<Control>("_slotsContainer").Value
            ?? merchantUi.GetNodeOrNull<Control>("%SlotsContainer");
        if (slots == null)
            return null;

        QueueMerchantShopVisualTuning(merchantUi, slots);

        Control? vanillaCharacter = traverse.Field<Control>("_characterCardContainer").Value;
        Control? vanillaColorless = traverse.Field<Control>("_colorlessCardContainer").Value;
        Control? vanillaRelics = traverse.Field<Control>("_relicContainer").Value;
        Control? vanillaPotions = traverse.Field<Control>("_potionContainer").Value;
        NMerchantCardRemoval? vanillaRemoval = traverse.Field<NMerchantCardRemoval>("_cardRemovalNode").Value;
        Control? dialogueRoot = merchantUi.GetNodeOrNull<Control>("%Dialogue");

        var ygoBuyPage = new Control
        {
            Name = "YgoAddonBuyPage",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop
        };
        ygoBuyPage.SetAnchorsPreset(LayoutPreset.FullRect);

        var gridCenter = new CenterContainer
        {
            Name = BuyGridCenterName,
            MouseFilter = MouseFilterEnum.Ignore
        };
        gridCenter.SetAnchorsPreset(LayoutPreset.FullRect);
        ApplyMerchantBuyGridCenterOffsets(gridCenter);

        var ygoGrid = new GridContainer
        {
            Name = "YgoAddonBuyGrid",
            Columns = YgoMerchantOfferGenerator.GridColumns,
            MouseFilter = MouseFilterEnum.Stop
        };
        ApplyYgoGridGapTheme(ygoGrid);
        gridCenter.AddChild(ygoGrid);
        ygoBuyPage.AddChild(gridCenter);

        for (int i = 0; i < ygoSlotCount; i++)
        {
            if (templateCard.Duplicate() is not NMerchantCard dup)
                continue;
            dup.Scale = Vector2.One * YgoMerchantShopLayoutTuning.MerchantSlotIdleScale;
            ygoGrid.AddChild(dup);
            dup.CustomMinimumSize = new Vector2(112, 198);
            dup.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            dup.SizeFlagsVertical = SizeFlags.ShrinkBegin;
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
            Name = AddonNavBarName,
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
            btnYgoSell,
            vanillaCharacter,
            vanillaColorless,
            vanillaRelics,
            vanillaPotions,
            vanillaRemoval,
            dialogueRoot);

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
        SetAddonNavBarVisible(merchantUi, false);
        return layer;
    }

    /// <summary>
    /// Tab bar must not stay visible or <see cref="Control.MouseFilterEnum.Stop"/> when the shop is closed:
    /// the inventory root uses <c>Ignore</c> but children still hit-test and steal clicks from the merchant room (e.g. Card Trader NPC).
    /// </summary>
    public static void SetAddonNavBarVisible(NMerchantInventory merchantUi, bool visible)
    {
        if (!GodotObject.IsInstanceValid(merchantUi))
            return;

        Control? nav = merchantUi.GetNodeOrNull<Control>(AddonNavBarName);
        if (nav == null)
            return;

        nav.Visible = visible;
        nav.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// <see cref="NMerchantInventory.Initialize"/> runs during room setup; touching <c>_slotsContainer</c> scale/position
    /// in the same frame can NRE before the native Control is ready. Run after a zero-delay tree timer tick.
    /// </summary>
    private static void QueueMerchantShopVisualTuning(NMerchantInventory merchantUi, Control slots)
    {
        SceneTree? tree = merchantUi.GetTree();
        if (tree == null)
        {
            ApplyMerchantShopVisualTuningCore(merchantUi, slots);
            return;
        }

        NMerchantInventory m = merchantUi;
        Control s = slots;
        SceneTreeTimer timer = tree.CreateTimer(0f);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(m) && GodotObject.IsInstanceValid(s))
                ApplyMerchantShopVisualTuningCore(m, s);
        };
    }

    /// <summary>
    /// Rug/carpet lives under vanilla <c>_slotsContainer</c>; optional child path for a carpet-only sprite.
    /// Applied once when YGO chrome mounts (relative to scene defaults at that moment).
    /// </summary>
    private static void ApplyMerchantShopVisualTuningCore(NMerchantInventory merchantUi, Control slots)
    {
        if (!GodotObject.IsInstanceValid(merchantUi) || !GodotObject.IsInstanceValid(slots))
            return;
        if (!slots.IsInsideTree())
            return;

        Vector2 sm = YgoMerchantShopLayoutTuning.MerchantSlotsContainerScaleMultiplier;
        Vector2 cur = slots.Scale;
        slots.Scale = new Vector2(cur.X * sm.X, cur.Y * sm.Y);
        slots.Position += YgoMerchantShopLayoutTuning.MerchantSlotsContainerPositionOffset;

        if (slots.FindChild(BuyGridCenterName, recursive: true, owned: false) is Control buyGridCenter)
            ApplyMerchantBuyGridCenterOffsets(buyGridCenter);

        string carpetPathStr = YgoMerchantShopLayoutTuning.MerchantCarpetOnlyNodePath.ToString();
        if (string.IsNullOrEmpty(carpetPathStr))
            return;
        if (merchantUi.GetNodeOrNull(YgoMerchantShopLayoutTuning.MerchantCarpetOnlyNodePath) is not Control rug
            || !GodotObject.IsInstanceValid(rug))
            return;
        Vector2 cm = YgoMerchantShopLayoutTuning.MerchantCarpetOnlyScaleMultiplier;
        Vector2 rcur = rug.Scale;
        rug.Scale = new Vector2(rcur.X * cm.X, rcur.Y * cm.Y);
        rug.Position += YgoMerchantShopLayoutTuning.MerchantCarpetOnlyPositionOffset;
    }

    /// <summary>
    /// Full-rect <see cref="CenterContainer"/> insets; nudging all four offsets by the same (X,Y) shifts the centered grid without resizing it.
    /// </summary>
    private static void ApplyMerchantBuyGridCenterOffsets(Control gridCenter)
    {
        const float pad = 20f;
        Vector2 o = YgoMerchantShopLayoutTuning.MerchantBuyGridPositionOffset;
        gridCenter.OffsetLeft = pad + o.X;
        gridCenter.OffsetTop = pad + o.Y;
        gridCenter.OffsetRight = -pad + o.X;
        gridCenter.OffsetBottom = -pad + o.Y;
    }

    private static void ApplyYgoGridGapTheme(GridContainer grid)
    {
        const int baseH = 18;
        const int baseV = 18;
        int h = Mathf.RoundToInt(baseH * YgoMerchantShopLayoutTuning.GridHorizontalGapMultiplier);
        int v = Mathf.RoundToInt(baseV * YgoMerchantShopLayoutTuning.GridVerticalGapMultiplier);
        grid.AddThemeConstantOverride("h_separation", Mathf.Max(0, h));
        grid.AddThemeConstantOverride("v_separation", Mathf.Max(0, v));
    }

    /// <summary>
    /// Opens the YGO buy grid tab (e.g. after entering shop from the Card Trader room NPC).
    /// </summary>
    public void ShowYgoBuyShopPage() => ApplyPage(ShopPage.YgoBuy);

    public static void TryShowYgoBuyPage(NMerchantInventory inv)
    {
        Control? slots = Traverse.Create(inv).Field<Control>("_slotsContainer").Value
            ?? inv.GetNodeOrNull<Control>("%SlotsContainer");
        slots?.GetNodeOrNull<YgoMerchantSlotsAddonLayer>(GodotName)?.ShowYgoBuyShopPage();
    }

    /// <summary>
    /// Restores vanilla merchant rows and hides YGO chrome. Call when the shop UI closes so the next open is not stuck on the Card Trader view.
    /// </summary>
    public void ResetToStandardShopPage() => ApplyPage(ShopPage.Standard);

    public static void TryResetToStandardShopPage(NMerchantInventory inv)
    {
        Control? slots = Traverse.Create(inv).Field<Control>("_slotsContainer").Value
            ?? inv.GetNodeOrNull<Control>("%SlotsContainer");
        slots?.GetNodeOrNull<YgoMerchantSlotsAddonLayer>(GodotName)?.ResetToStandardShopPage();
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

        CallDeferred(nameof(DeferredRefreshYgoBuyCardVisuals));
    }

    private void DeferredRefreshYgoBuyCardVisuals()
    {
        RefreshYgoBuyNCardVisuals();
        if (YgoMerchantShopLayoutTuning.RunOneFrameHoverScalePulseAfterLayout)
            _ = RunYgoBuyHoverScalePulseAsync();
    }

    private void RefreshYgoBuyNCardVisuals()
    {
        foreach (Node ch in _ygoGrid.GetChildren())
        {
            if (ch is not NMerchantCard slot)
                continue;
            Control? holder = slot.GetNodeOrNull<Control>("%CardHolder");
            if (holder == null)
                continue;
            foreach (Node cn in holder.GetChildren())
            {
                if (cn is NCard nc)
                {
                    nc.Scale = Vector2.One;
                    nc.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                }
            }
        }
    }

    private async Task RunYgoBuyHoverScalePulseAsync()
    {
        if (!IsInsideTree())
            return;

        SceneTree? tree = GetTree();
        if (tree == null)
            return;

        float hover = YgoMerchantShopLayoutTuning.MerchantSlotHoverScale;
        float idle = YgoMerchantShopLayoutTuning.MerchantSlotIdleScale;
        foreach (Node ch in _ygoGrid.GetChildren())
        {
            if (ch is NMerchantCard slot)
                slot.Scale = Vector2.One * hover;
        }

        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        if (!IsInsideTree())
            return;

        foreach (Node ch in _ygoGrid.GetChildren())
        {
            if (ch is NMerchantCard slot)
                slot.Scale = Vector2.One * idle;
        }

        RefreshYgoBuyNCardVisuals();
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
                SetVanillaMerchantSlotRowsVisible(true);
                SetMerchantDialogueRootVisible(true);
                Visible = false;
                _ygoBuyPage.Visible = false;
                _ygoSellPage.Visible = false;
                break;
            case ShopPage.YgoBuy:
                SetVanillaMerchantSlotRowsVisible(false);
                SetMerchantDialogueRootVisible(false);
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

                    CallDeferred(nameof(DeferredRefreshYgoBuyCardVisuals));
                }

                break;
            case ShopPage.YgoSell:
                SetVanillaMerchantSlotRowsVisible(false);
                SetMerchantDialogueRootVisible(false);
                Visible = true;
                _ygoBuyPage.Visible = false;
                _ygoSellPage.Visible = true;
                if (enteringSell)
                    RebuildSellList();
                break;
        }

        UpdateNavigationMethod.Invoke(_merchantUi, null);
    }

    private void SetVanillaMerchantSlotRowsVisible(bool visible)
    {
        if (_vanillaCharacterCards != null)
            _vanillaCharacterCards.Visible = visible;
        if (_vanillaColorlessCards != null)
            _vanillaColorlessCards.Visible = visible;
        if (_vanillaRelics != null)
            _vanillaRelics.Visible = visible;
        if (_vanillaPotions != null)
            _vanillaPotions.Visible = visible;
        if (_vanillaCardRemoval != null)
            _vanillaCardRemoval.Visible = visible;
    }

    private void SetMerchantDialogueRootVisible(bool visible)
    {
        if (_merchantDialogueRoot != null)
            _merchantDialogueRoot.Visible = visible;
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
        SetVanillaMerchantSlotRowsVisible(true);
        SetMerchantDialogueRootVisible(true);
        if (_sidecar == null)
            return;
        foreach (MerchantCardEntry e in _sidecar.YgoCardEntries)
            e.PurchaseCompleted -= _sidecar.PurchaseUpdateHandler;
        _sidecar = null;
    }
}
