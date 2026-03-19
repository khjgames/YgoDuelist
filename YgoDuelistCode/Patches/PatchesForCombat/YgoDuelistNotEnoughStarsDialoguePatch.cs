using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Thought-bubble line when the local Duelist tries to play a card but lacks star cost uses Conduit wording.
/// </summary>
[HarmonyPatch]
public static class YgoDuelistNotEnoughStarsDialoguePatch
{
    static MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Cards.UnplayableReasonExtensions")
            ?? throw new InvalidOperationException("UnplayableReasonExtensions not found.");
        return AccessTools.Method(type, "GetPlayerDialogueLine", new[] { typeof(UnplayableReason), typeof(AbstractModel) })
            ?? throw new InvalidOperationException("GetPlayerDialogueLine not found.");
    }

    [HarmonyPrefix]
    public static bool Prefix(UnplayableReason reason, ref LocString? __result)
    {
        if (!reason.HasFlag(UnplayableReason.StarCostTooHigh))
            return true;

        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null)
            return true;

        var me = LocalContext.GetMe(state);
        if (me?.Character is not global::YgoDuelist.YgoDuelistCode.Character.YgoDuelist)
            return true;

        __result = new LocString("combat_messages", "NOT_ENOUGH_CONDUIT");
        return false;
    }
}
