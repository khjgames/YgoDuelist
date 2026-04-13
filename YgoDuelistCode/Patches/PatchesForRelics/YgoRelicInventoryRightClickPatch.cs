using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Vanilla relic holders only fire <see cref="NClickableControl.SignalName.Released"/> for left mouse; zone relics also run their action on right-click.
/// </summary>
[HarmonyPatch(typeof(NRelicInventoryHolder), "_Ready")]
public static class YgoRelicInventoryRightClickPatch
{
    private static readonly System.Reflection.MethodInfo? OnRelicClicked =
        AccessTools.Method(typeof(NRelicInventory), "OnRelicClicked", new[] { typeof(RelicModel) });

    [HarmonyPostfix]
    public static void Postfix(NRelicInventoryHolder __instance)
    {
        __instance.Connect(
            NClickableControl.SignalName.MouseReleased,
            Callable.From<InputEvent>(ev => OnMouseReleased(__instance, ev)));
    }

    private static void OnMouseReleased(NRelicInventoryHolder holder, InputEvent ev)
    {
        if (ev is not InputEventMouseButton mb || mb.Pressed || mb.ButtonIndex != MouseButton.Right)
            return;
        var model = holder.Relic?.Model;
        if (model == null || !IsYgoZoneInteractionRelic(model))
            return;
        var inventory = holder.Inventory;
        if (inventory == null || OnRelicClicked == null)
            return;
        OnRelicClicked.Invoke(inventory, new object[] { model });
    }

    private static bool IsYgoZoneInteractionRelic(RelicModel model) =>
        SpellTrapZoneRelic.IsSpellTrapZoneRelic(model)
        || GraveyardRelic.IsGraveyardRelic(model)
        || BanishedRelic.IsBanishedRelic(model)
        || ExtraDeckRelic.IsExtraDeckRelic(model)
        || TrunkSideDeckRelic.IsTrunkSideDeckRelic(model);
}
