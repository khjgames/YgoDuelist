using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForCombat;

/// <summary>
/// YGO duel pets are removed via <see cref="Patches.DuelMonsterPetDeathPatch"/> (<c>QueueFree</c> on <see cref="NCreature"/>)
/// while the same <see cref="MegaCrit.Sts2.Core.GameActions.PlayCardAction"/> can still run block UI updates (tribute
/// summon, option-pile Needle Ball, etc.). Godot may dispose child controls before <see cref="NHealthBar.RefreshBlockUi"/>
/// runs, causing <see cref="System.ObjectDisposedException"/> on <c>NinePatchRect.SetVisible</c>.
/// </summary>
internal static class NHealthBarDisposedUiGuard
{
    internal static bool HealthBarUiSafe(NHealthBar bar)
    {
        if (!GodotObject.IsInstanceValid(bar))
            return false;

        Traverse t = Traverse.Create(bar);
        return IsLiveControl(t.Field<Control>("_blockContainer").Value)
               && IsLiveControl(t.Field<Control>("_blockOutline").Value)
               && IsLiveControl(t.Field<Control>("_hpForeground").Value);
    }

    private static bool IsLiveControl(Control? c) => c != null && GodotObject.IsInstanceValid(c);
}

[HarmonyPatch(typeof(NHealthBar), "RefreshBlockUi")]
public static class NHealthBarRefreshBlockUiDisposedGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NHealthBar __instance) => NHealthBarDisposedUiGuard.HealthBarUiSafe(__instance);
}

[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.AnimateInBlock))]
public static class NHealthBarAnimateInBlockDisposedGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NHealthBar __instance) => NHealthBarDisposedUiGuard.HealthBarUiSafe(__instance);
}
