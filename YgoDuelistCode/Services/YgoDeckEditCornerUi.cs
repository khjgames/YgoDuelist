using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Rewards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared bottom-left “Edit your Deck” loot row for rest site and merchant room. Uses top-left positioning inside a
/// full-rect layer so the control is not clipped (bottom-left anchor-only layouts often end up with zero height).
/// </summary>
public static class YgoDeckEditCornerUi
{
    public const string ButtonName = "YgoDeckEditCornerBtn";

    public const string LayerNameRest = "YgoCampfireDeckEditCornerLayer";

    public const string LayerNameShop = "YgoDeckEditCornerLayerShop";

    private static Control? _registeredRestLayer;

    private static Control? _registeredShopLayer;

    /// <summary>Hides the bottom-left deck edit loot row while the deck edit overlay is open.</summary>
    public static void SetDeckEditMenuOpen(bool menuOpen)
    {
        bool showCorner = !menuOpen;
        ApplyCornerLayerVisibility(_registeredRestLayer, showCorner);

        NMerchantRoom? merchant = NMerchantRoom.Instance;
        bool shopOk = merchant != null && !merchant.Inventory.IsOpen;
        ApplyCornerLayerVisibility(_registeredShopLayer, showCorner && shopOk);
    }

    private static void ApplyCornerLayerVisibility(Control? layer, bool visible)
    {
        if (layer != null && GodotObject.IsInstanceValid(layer))
            layer.Visible = visible;
    }

    private static void RegisterCornerLayer(string layerName, Control layer)
    {
        if (layerName == LayerNameRest)
            _registeredRestLayer = layer;
        else if (layerName == LayerNameShop)
            _registeredShopLayer = layer;
    }

    private static void ClearCornerLayerRef(string layerName)
    {
        if (layerName == LayerNameRest)
            _registeredRestLayer = null;
        else if (layerName == LayerNameShop)
            _registeredShopLayer = null;
    }

    public static void EnsureRestSite(NRestSiteRoom room, Player player) =>
        Ensure(room, player, LayerNameRest, YgoCampfireDeckEditLayout.RestSiteCornerOffsetX,
            YgoCampfireDeckEditLayout.RestSiteCornerOffsetY, "RestSite");

    public static void EnsureShop(NMerchantRoom room, Player player)
    {
        Ensure(room, player, LayerNameShop, YgoCampfireDeckEditLayout.ShopCornerOffsetX,
            YgoCampfireDeckEditLayout.ShopCornerOffsetY, "Shop");
        Control? layer = room.GetNodeOrNull<Control>(LayerNameShop);
        if (layer != null)
            layer.Visible = !room.Inventory.IsOpen;
    }

    public static void SetShopLayerVisible(bool visible)
    {
        NMerchantRoom? room = NMerchantRoom.Instance;
        Control? layer = room?.GetNodeOrNull<Control>(LayerNameShop);
        if (layer == null || !GodotObject.IsInstanceValid(layer) || layer.Visible == visible)
            return;

        layer.Visible = visible;
        Log($"Shop layer Visible set to {visible}");
    }

    private static void Ensure(Control host, Player player, string layerName, float offsetX, float offsetY, string tag)
    {
        if (!host.IsInsideTree())
        {
            Log($"{tag}: host not in tree, skip");
            return;
        }

        if (!YgoPlayerRunPiles.IsYgoRunPlayer(player))
        {
            ClearCornerLayerRef(layerName);
            host.GetNodeOrNull<Control>(layerName)?.QueueFree();
            Log($"{tag}: not Ygo Duelist, removed layer if any");
            return;
        }

        YgoCampfireDeckEditCharges.EnsureInitializedForRestSiteUi(player);

        Control layer = host.GetNodeOrNull<Control>(layerName) ?? CreateLayer(host, layerName, tag);
        RegisterCornerLayer(layerName, layer);
        if (!layer.HasMeta("ygo_deck_ctx"))
            layer.SetMeta("ygo_deck_ctx", tag);

        NRewardButton? btn = layer.GetNodeOrNull<NRewardButton>(ButtonName);
        if (btn == null)
        {
            var reward = new YgoCampfireDeckEditUiReward(
                player,
                new LocString("combat_messages", "YGODUELIST-CAMPFIRE_DECK_EDIT.corner_button"),
                async () =>
                {
                    NYgoCampfireDeckEditMenuScreen.Push(player);
                    await Task.CompletedTask;
                });

            btn = NRewardButton.Create(reward, null!);
            btn.Name = ButtonName;
            btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            btn.CustomMinimumSize = Vector2.Zero;
            layer.AddChild(btn);
            Log($"{tag}: created NRewardButton, parent size={host.Size}");
        }

        Callable.From(() => ApplyLayout(layer, btn, offsetX, offsetY, tag)).CallDeferred();
    }

    private static Control CreateLayer(Control host, string layerName, string tag)
    {
        var layer = new Control
        {
            Name = layerName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = YgoCampfireDeckEditLayout.CornerLayerZIndex,
            ZAsRelative = false
        };
        layer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        host.AddChild(layer);
        host.MoveChild(layer, host.GetChildCount() - 1);
        layer.SetMeta("ygo_deck_ctx", tag);
        layer.Resized += () =>
        {
            NRewardButton? b = layer.GetNodeOrNull<NRewardButton>(ButtonName);
            if (b != null)
            {
                (float ox, float oy) = GetOffsetsForLayer(layer.Name);
                string ctx = layer.HasMeta("ygo_deck_ctx") ? layer.GetMeta("ygo_deck_ctx").AsString() : "?";
                ApplyLayout(layer, b, ox, oy, ctx);
            }
        };
        Log($"{tag}: created layer ZIndex={layer.ZIndex}");
        return layer;
    }

    private static (float X, float Y) GetOffsetsForLayer(string name) =>
        name == LayerNameShop
            ? (YgoCampfireDeckEditLayout.ShopCornerOffsetX, YgoCampfireDeckEditLayout.ShopCornerOffsetY)
            : (YgoCampfireDeckEditLayout.RestSiteCornerOffsetX, YgoCampfireDeckEditLayout.RestSiteCornerOffsetY);

    private static void ApplyLayout(Control layer, NRewardButton btn, float offsetX, float offsetY, string tag,
        bool allowRetry = true)
    {
        if (!GodotObject.IsInstanceValid(layer) || !GodotObject.IsInstanceValid(btn))
            return;

        float h = layer.Size.Y;
        float w = layer.Size.X;
        if (h < 4f || w < 4f)
        {
            if (allowRetry)
            {
                Log($"{tag}: ApplyLayout retry next frame — layer not sized yet ({w:0.}x{h:0.})");
                Callable.From(() => ApplyLayout(layer, btn, offsetX, offsetY, tag, allowRetry: false)).CallDeferred();
            }
            else
                Log($"{tag}: ApplyLayout failed — layer still tiny ({w:0.}x{h:0.})");
            return;
        }

        float pad = YgoCampfireDeckEditLayout.CornerButtonHorizontalPaddingPixels;
        float floor = YgoCampfireDeckEditLayout.CornerButtonMinWidthFloorPixels;
        float btnW = YgoCampfireDeckEditLayout.CornerButtonFixedWidthPixels + pad;
        if (floor > 0f)
            btnW = Mathf.Max(btnW, floor);
        float btnH = YgoCampfireDeckEditLayout.CornerButtonFixedHeightPixels;

        btn.CustomMinimumSize = new Vector2(btnW, btnH);
        btn.ResetSize();
        btn.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        float placeH = btn.Size.Y > 0.5f ? btn.Size.Y : btnH;
        float y = h - offsetY - placeH;
        btn.Position = new Vector2(offsetX, y);
        btn.Visible = true;
        Log($"{tag}: ApplyLayout host={layer.GetParent()?.GetType().Name} layer={w:0.}x{h:0.} customMin=({btnW:0.}x{btnH:0.}) size={btn.Size.X:0.}x{btn.Size.Y:0.}");
    }

    private static void Log(string message)
    {
        if (YgoCampfireDeckEditLayout.DebugLogCornerUi)
            GD.Print($"[YgoDeckEditCorner] {message}");
    }
}
