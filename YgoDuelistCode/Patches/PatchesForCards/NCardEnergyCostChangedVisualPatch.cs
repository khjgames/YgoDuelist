using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Vanilla <see cref="NCard"/> subscribes to Affliction/Enchantment changes but not <see cref="CardModel.EnergyCostChanged"/>.
/// Ygo monsters invalidate cached <see cref="CardModel.EnergyCost"/> when attack/defense/hand-effect form changes; without a UI hook
/// the orb number can stay stale. Refresh label (and <see cref="NCard"/>.Reload for Ygo energy icon postfixes) on that event.
/// </summary>
internal static class NCardEnergyCostSubscriptionState
{
    private static readonly ConditionalWeakTable<NCard, Entry> Table = new();

    private sealed class Entry
    {
        public CardModel? Model;
        public Action? Handler;
    }

    private static readonly MethodInfo? NCardReload =
        typeof(NCard).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance);

    internal static void Subscribe(NCard ncard, CardModel? model)
    {
        if (model == null || !ncard.IsInsideTree())
            return;

        if (!Table.TryGetValue(ncard, out var entry))
        {
            entry = new Entry();
            Table.Add(ncard, entry);
        }
        if (entry.Model == model && entry.Handler != null)
            return;

        if (entry.Model != null && entry.Handler != null)
            entry.Model.EnergyCostChanged -= entry.Handler;

        void Handler()
        {
            if (!GodotObject.IsInstanceValid(ncard) || ncard.Model != model)
                return;
            ncard.UpdateVisuals(ncard.DisplayingPile, CardPreviewMode.Normal);
            NCardReload?.Invoke(ncard, null);
        }

        entry.Model = model;
        entry.Handler = Handler;
        model.EnergyCostChanged += Handler;
    }

    internal static void Unsubscribe(NCard ncard, CardModel? model)
    {
        if (model == null)
            return;
        if (!Table.TryGetValue(ncard, out var entry))
            return;
        if (entry.Model != model || entry.Handler == null)
            return;

        model.EnergyCostChanged -= entry.Handler;
        entry.Model = null;
        entry.Handler = null;
    }
}

[HarmonyPatch(typeof(NCard), "SubscribeToModel", new[] { typeof(CardModel) })]
public static class NCardSubscribeToModelEnergyCostPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCard __instance, CardModel? model)
    {
        NCardEnergyCostSubscriptionState.Subscribe(__instance, model);
    }
}

[HarmonyPatch(typeof(NCard), "UnsubscribeFromModel", new[] { typeof(CardModel) })]
public static class NCardUnsubscribeFromModelEnergyCostPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCard __instance, CardModel? model)
    {
        NCardEnergyCostSubscriptionState.Unsubscribe(__instance, model);
    }
}
