using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
public static class CardPileAddInternalBanishedDdScoutPatch
{
    static void Postfix(CardPile __instance, CardModel card)
    {
        if (__instance.Type != BanishedPile.CustomType || !__instance.IsCombatPile)
            return;
        if (card is not D_D_Scout_Plane plane)
            return;

        CombatState? cs = CombatManager.Instance?.DebugOnlyGetState();
        if (cs == null)
            return;

        foreach (Player p in cs.Players)
        {
            if (BanishedPile.CustomType.GetPile(p) == __instance)
            {
                YgoDdScoutPlaneEndPhase.OnAddedToBanishedPile(p, plane);
                return;
            }
        }
    }
}
