using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When targeting enemies (AnyEnemy), hovering the local player's creature still calls HighlightPlayer
/// in OnFocus, showing a yellow square. For enemy-only targeting we should not show that highlight
/// on the player. After OnFocus we unhighlight when we're in selection for AnyEnemy and this creature is not an enemy.
/// </summary>
[HarmonyPatch(typeof(NCreature), "OnFocus")]
public static class NCreatureOptionHolderTargetingNoPlayerHighlightPatch
{
    private static readonly FieldInfo ValidTargetsTypeField =
        AccessTools.Field(typeof(NTargetManager), "_validTargetsType");

    static void Postfix(NCreature __instance)
    {
        if (__instance?.Entity == null)
            return;
        var tm = NTargetManager.Instance;
        if (tm == null || !tm.IsInSelection)
            return;
        var targetType = ValidTargetsTypeField?.GetValue(tm);
        if (targetType == null || (TargetType)targetType != TargetType.AnyEnemy)
            return;
        if (__instance.Entity.Side == CombatSide.Enemy)
            return;

        NRun.Instance?.GlobalUi?.MultiplayerPlayerContainer?.UnhighlightPlayer(__instance.Entity.Player);
    }
}
