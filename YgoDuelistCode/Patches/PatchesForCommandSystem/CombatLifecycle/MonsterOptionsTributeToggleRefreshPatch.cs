using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
public static class CardPileHandAddMonsterOptionsRefreshPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardPile __instance, CardModel card, int index, bool silent)
    {
        if (__instance.Type != PileType.Hand || silent || card?.Owner == null)
            return;
        DuelMonsterMonsterOptionsMenu.RequestDeferredTryRefresh(card.Owner);
    }
}

[HarmonyPatch(typeof(CardPile), nameof(CardPile.RemoveInternal))]
public static class CardPileHandRemoveMonsterOptionsRefreshPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardPile __instance, CardModel card, bool silent)
    {
        if (__instance.Type != PileType.Hand || silent || card?.Owner == null)
            return;
        DuelMonsterMonsterOptionsMenu.RequestDeferredTryRefresh(card.Owner);
    }
}

[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.Energy), MethodType.Setter)]
public static class PlayerCombatStateEnergyMonsterOptionsRefreshPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerCombatState __instance)
    {
        var player = CombatManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault(p => p.PlayerCombatState == __instance);
        if (player != null)
            DuelMonsterMonsterOptionsMenu.RequestDeferredTryRefresh(player);
    }
}

[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.Stars), MethodType.Setter)]
public static class PlayerCombatStateStarsMonsterOptionsRefreshPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerCombatState __instance)
    {
        var player = CombatManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault(p => p.PlayerCombatState == __instance);
        if (player != null)
            DuelMonsterMonsterOptionsMenu.RequestDeferredTryRefresh(player);
    }
}
