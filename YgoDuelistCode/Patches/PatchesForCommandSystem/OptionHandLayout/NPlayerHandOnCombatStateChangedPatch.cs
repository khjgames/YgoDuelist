using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Prevents ObjectDisposedException when a deferred CombatStateChanged runs after we've
/// torn down option holders (e.g. user clicked Exit while dragging). The game iterates
/// Holders and _holdersAwaitingQueue and calls UpdateCard(); we skip any holder that
/// is already disposed (QueueFree'd).
/// </summary>
[HarmonyPatch(typeof(NPlayerHand))]
public static class NPlayerHandOnCombatStateChangedPatch
{
    static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(NPlayerHand), "OnCombatStateChanged");
    }

    static bool Prefix(NPlayerHand __instance, CombatState state)
    {
        var combatStateField = AccessTools.Field(typeof(NPlayerHand), "_combatState");
        combatStateField?.SetValue(__instance, state);

        var container = __instance.CardHolderContainer;
        foreach (NHandCardHolder holder in container.GetChildren().OfType<NHandCardHolder>())
        {
            if (GodotObject.IsInstanceValid(holder))
                holder.UpdateCard();
        }

        var queueField = AccessTools.Field(typeof(NPlayerHand), "_holdersAwaitingQueue");
        var queue = queueField?.GetValue(__instance) as Dictionary<NHandCardHolder, int>;
        if (queue != null)
        {
            foreach (NHandCardHolder key in queue.Keys.ToList())
            {
                if (GodotObject.IsInstanceValid(key))
                    key.UpdateCard();
            }
        }

        var selectedContainerField = AccessTools.Field(typeof(NPlayerHand), "_selectedHandCardContainer");
        var selectedContainer = selectedContainerField?.GetValue(__instance);
        if (selectedContainer != null)
        {
            var holdersProp = AccessTools.Property(selectedContainer.GetType(), "Holders");
            var holderList = holdersProp?.GetValue(selectedContainer) as System.Collections.IEnumerable;
            if (holderList != null)
            {
                foreach (var h in holderList)
                {
                    if (h is NSelectedHandCardHolder holder2 && GodotObject.IsInstanceValid(holder2))
                        holder2.CardNode?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
                }
            }
        }

        var updateMethod = AccessTools.DeclaredMethod(typeof(NPlayerHand), "UpdateHandDisabledState");
        updateMethod?.Invoke(__instance, new object[] { state });

        return false;
    }
}
