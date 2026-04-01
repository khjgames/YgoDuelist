using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// After a <see cref="PlayCardAction"/> for a card in the Spell/Trap zone pile finishes, force a second-hand
/// republish from the live zone. Vanilla sync can skip UI when the cached reference list matches the pile even
/// though holders/NCards are stale (used set cards still drawn, wrong count).
/// Option-pile plays use <see cref="PlayCardFromOptionPilePatch"/> instead; this patch only handles
/// <see cref="SpellTrapZonePile"/>.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
public static class PlayCardActionSecondHandPostPlayRefreshPatch
{
    [HarmonyPrefix]
    private static void CaptureStartPile(PlayCardAction __instance, ref object? __state)
    {
        __state = null;
        var c = __instance?.NetCombatCard.ToCardModel();
        if (c?.Pile != null)
            __state = c.Pile.Type;
    }

    [HarmonyPostfix]
    private static void AfterPlayScheduleSpellTrapSecondHandRefresh(PlayCardAction __instance, Task __result, object? __state)
    {
        if (__state is not PileType fromPile || __result == null)
            return;
        if (fromPile != SpellTrapZonePile.CustomType)
            return;

        var player = __instance.Player;
        if (player == null)
            return;

        _ = __result.ContinueWith(
            _ =>
            {
                Callable.From(() => YgoSpellTrapZoneBridge.ForceRefreshSpellTrapSecondHandFromZone(player))
                    .CallDeferred();
            },
            TaskContinuationOptions.None);
    }
}
