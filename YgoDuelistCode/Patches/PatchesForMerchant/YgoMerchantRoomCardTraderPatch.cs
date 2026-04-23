using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YgoDuelist;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// Adds a room-world Card Trader <see cref="TextureButton"/> (<c>card_trader_npc_bland.png</c> default, <c>card_trader_npc.png</c> on hover/focus) under <c>images/card_frames/</c>, to the left of the vanilla merchant NPC,
/// with a configurable gap/scale/offset (<see cref="YgoMerchantShopLayoutTuning"/>); opens the shared <see cref="NMerchantInventory"/> on the YGO buy page.
/// </summary>
[HarmonyPatch(typeof(NMerchantRoom), nameof(NMerchantRoom._Ready))]
public static class YgoMerchantRoomCardTraderReadyPatch
{
    public const string CardTraderButtonName = "YgoCardTraderRoomNpc";
    private const string MetaKey = "YgoCardTraderRoomNpc_v1";

    [HarmonyPostfix]
    public static void Postfix(NMerchantRoom __instance)
    {
        if (__instance.HasMeta(MetaKey))
            return;

        MerchantInventory? invModel = __instance.Inventory?.Inventory;
        if (invModel == null || !YgoPlayerRunPiles.IsYgoRunPlayer(invModel.Player))
            return;

        string relBland = "card_frames/card_trader_npc_bland.png".ImagePath().Replace('\\', '/');
        string resBland = relBland.StartsWith("res://", System.StringComparison.Ordinal) ? relBland : "res://" + relBland;
        Texture2D? texBland = ResourceLoader.Load<Texture2D>(resBland, null, ResourceLoader.CacheMode.Reuse);
        if (texBland == null)
        {
            MainFile.Logger.Warn($"[YgoDuelist] Card Trader NPC default (bland) image missing at {resBland}; room NPC not created.");
            return;
        }

        string relVivid = "card_frames/card_trader_npc.png".ImagePath().Replace('\\', '/');
        string resVivid = relVivid.StartsWith("res://", System.StringComparison.Ordinal) ? relVivid : "res://" + relVivid;
        Texture2D? texVivid = ResourceLoader.Load<Texture2D>(resVivid, null, ResourceLoader.CacheMode.Reuse);

        NMerchantButton merchant = __instance.MerchantButton;
        Control parent = (Control)merchant.GetParent();

        var btn = new TextureButton
        {
            Name = CardTraderButtonName,
            TextureNormal = texBland,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            FocusMode = Control.FocusModeEnum.All,
            MouseDefaultCursorShape = Control.CursorShape.Arrow
        };
        if (texVivid != null)
        {
            btn.TextureHover = texVivid;
            btn.TextureFocused = texVivid;
        }

        parent.AddChild(btn);
        parent.MoveChild(btn, merchant.GetIndex());

        __instance.SetMeta(MetaKey, true);

        btn.Pressed += () => OnCardTraderPressed(__instance, btn);

        NMerchantInventory invUi = __instance.Inventory;
        invUi.Connect(
            NMerchantInventory.SignalName.InventoryClosed,
            Callable.From(() =>
            {
                TextureButton? b = __instance.GetNodeOrNull<TextureButton>(CardTraderButtonName);
                if (b == null || !GodotObject.IsInstanceValid(__instance))
                    return;
                SyncCardTraderDisabled(__instance, b);
                SceneTree? tree = __instance.GetTree();
                if (tree == null)
                    return;
                NMerchantRoom roomRef = __instance;
                SceneTreeTimer t = tree.CreateTimer(0f);
                t.Timeout += () =>
                {
                    TextureButton? b2 = roomRef.GetNodeOrNull<TextureButton>(CardTraderButtonName);
                    if (b2 != null && GodotObject.IsInstanceValid(roomRef))
                        SyncCardTraderDisabled(roomRef, b2);
                };
            }));

        SceneTreeTimer layoutTimer = __instance.GetTree().CreateTimer(0f);
        layoutTimer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(__instance))
                LayoutAndSyncCardTrader(__instance, btn, merchant);
        };
    }

    private static void OnCardTraderPressed(NMerchantRoom room, TextureButton btn)
    {
        if (room.MerchantButton.IsLocalPlayerDead)
            return;

        if (room.Inventory.IsOpen)
        {
            YgoMerchantSlotsAddonLayer.TryShowYgoBuyPage(room.Inventory);
            return;
        }

        room.OpenInventory();
        if (!room.Inventory.IsOpen)
        {
            SyncCardTraderDisabled(room, btn);
            return;
        }

        YgoMerchantSlotsAddonLayer.TryShowYgoBuyPage(room.Inventory);
    }

    private static void LayoutAndSyncCardTrader(NMerchantRoom room, TextureButton trader, NMerchantButton merchant)
    {
        if (!GodotObject.IsInstanceValid(room) || !GodotObject.IsInstanceValid(trader) || !GodotObject.IsInstanceValid(merchant))
            return;

        Rect2 gr = merchant.GetGlobalRect();
        Vector2 sm = YgoMerchantShopLayoutTuning.CardTraderRoomNpcScaleMultiplier;
        float gap = YgoMerchantShopLayoutTuning.CardTraderRoomNpcGapPixels;
        Vector2 scaledFootprint = new(
            gr.Size.X * Mathf.Abs(sm.X),
            gr.Size.Y * Mathf.Abs(sm.Y));

        trader.Size = gr.Size;
        trader.Scale = sm;
        Vector2 ourTopLeft = new Vector2(
            gr.Position.X - gap - scaledFootprint.X,
            gr.Position.Y) + YgoMerchantShopLayoutTuning.CardTraderRoomNpcPositionOffset;
        trader.GlobalPosition = ourTopLeft;

        SyncCardTraderDisabled(room, trader);
    }

    public static void SyncCardTraderDisabled(NMerchantRoom room, TextureButton trader)
    {
        bool isCurrent = ActiveScreenContext.Instance.IsCurrent(room);
        trader.Disabled = !isCurrent || room.Inventory.IsOpen;
    }
}

[HarmonyPatch(typeof(NMerchantRoom), "OnActiveScreenUpdated")]
public static class YgoMerchantRoomCardTraderActiveScreenPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMerchantRoom __instance)
    {
        TextureButton? btn = __instance.GetNodeOrNull<TextureButton>(YgoMerchantRoomCardTraderReadyPatch.CardTraderButtonName);
        if (btn == null)
            return;

        YgoMerchantRoomCardTraderReadyPatch.SyncCardTraderDisabled(__instance, btn);
    }
}
