using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Adds a small, hover-zoomable preview of the top draw card when
/// Convulsion Of Nature is active. The preview sits just above the draw pile
/// and energy counter at the bottom-left of the combat UI.
/// </summary>
[HarmonyPatch]
public static class ConvulsionOfNaturePreviewUiPatch
{
    private const float PreviewScale = 0.36f; // 20% smaller than second-hand scale (0.45 * 0.8)
    private static readonly Vector2 PreviewOffsetFromDrawPile = new(140f, -250f);

    private static NYgoConvulsionPreviewHolder? _preview;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatUi), "Activate")]
    private static void OnCombatUiActivate(NCombatUi __instance)
    {
        EnsurePreviewExists(__instance);
        Sync(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatUi), "Enable")]
    private static void OnCombatUiEnable(NCombatUi __instance)
    {
        Sync(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
    private static void OnCombatRoomExit(NCombatRoom __instance)
    {
        TearDown();
    }

    private static void EnsurePreviewExists(NCombatUi ui)
    {
        if (_preview != null && GodotObject.IsInstanceValid(_preview))
            return;

        _preview = NYgoConvulsionPreviewHolder.Create();
        _preview.Scale = Vector2.One * PreviewScale;
        _preview.RotationDegrees = 0f;
        _preview.ZIndex = 2;
        _preview.Visible = false;
        NCombatUi uiRef = ui;
        _preview.ConvulsionSyncTick = () => Sync(uiRef);
        ui.AddChildSafely(_preview);
        _preview.Owner = ui;
        _preview.SetProcess(true);
    }

    private static void TearDown()
    {
        if (_preview != null && GodotObject.IsInstanceValid(_preview))
        {
            _preview.ConvulsionSyncTick = null;
            _preview.SetProcess(false);
            if (_preview.IsInsideTree())
                _preview.QueueFree();
        }

        _preview = null;
    }

    private static void Sync(NCombatUi ui)
    {
        if (ui == null)
            return;

        EnsurePreviewExists(ui);
        if (_preview == null || !GodotObject.IsInstanceValid(_preview))
            return;

        var stateField = AccessTools.Field(typeof(NCombatUi), "_state");
        var state = stateField?.GetValue(ui) as MegaCrit.Sts2.Core.Combat.CombatState;
        if (state == null)
        {
            _preview.Visible = false;
            return;
        }

        Player? me = LocalContext.GetMe(state.Players);
        if (me?.Creature == null)
        {
            _preview.Visible = false;
            return;
        }

        // "Control the card" = you currently have Convulsion of Nature face-up in your Spell/Trap zone pile.
        var zonePile = YgoPlayerPiles.SpellTrapZone(me);
        bool enabled = zonePile != null
            && zonePile.Cards.Any(c =>
                c is IYgoConvulsionDrawPilePreviewSource preview && preview.IsFaceUpActiveForConvulsionDrawPreview());
        var draw = me.PlayerCombatState?.DrawPile;
        if (!enabled || draw == null || draw.IsEmpty)
        {
            _preview.Visible = false;
            return;
        }

        _preview.EnsureCard(draw.Cards[0]);

        // Anchor relative to the draw pile button (bottom-left UI element).
        Vector2 drawPileGlobal = ui.DrawPile.GlobalPosition;
        _preview.GlobalPosition = drawPileGlobal + PreviewOffsetFromDrawPile;
        _preview.Visible = true;
        if (_preview.Hitbox != null)
        {
            _preview.Hitbox.Visible = true;
            _preview.Hitbox.SetEnabled(true);
        }
    }
}
