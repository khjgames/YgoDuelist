using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelistCharacter = global::YgoDuelist.YgoDuelistCode.Character.YgoDuelist;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Thought-bubble when the local Duelist cannot pay star cost: use Conduit wording.
/// Must match extension method <c>GetPlayerDialogueLine(UnplayableReason, AbstractModel?)</c> parameter list exactly for Harmony.
/// </summary>
[HarmonyPatch]
public static class YgoDuelistNotEnoughStarsDialoguePatch
{
    static MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Cards.UnplayableReasonExtensions")
            ?? throw new InvalidOperationException("UnplayableReasonExtensions not found.");
        return AccessTools.Method(type, "GetPlayerDialogueLine", new[] { typeof(UnplayableReason), typeof(AbstractModel) })
            ?? throw new InvalidOperationException("GetPlayerDialogueLine(UnplayableReason, AbstractModel) not found.");
    }

    [HarmonyPrefix]
    public static bool Prefix(UnplayableReason reason, AbstractModel? preventer, ref LocString? __result)
    {
        GD.Print($"[YgoDuelist NotEnoughConduit] Prefix reason={reason} preventer={(preventer == null ? "null" : preventer.GetType().Name)}");

        if (!reason.HasFlag(UnplayableReason.StarCostTooHigh))
        {
            GD.Print("[YgoDuelist NotEnoughConduit] skip: not StarCostTooHigh");
            return true;
        }

        var state = CombatManager.Instance.DebugOnlyGetState();
        if (state == null)
        {
            GD.Print("[YgoDuelist NotEnoughConduit] skip: CombatState null");
            return true;
        }

        var me = LocalContext.GetMe(state);
        if (me?.Character is not YgoDuelistCharacter)
        {
            GD.Print($"[YgoDuelist NotEnoughConduit] skip: character={(me?.Character == null ? "null" : me.Character.GetType().Name)}");
            return true;
        }

        __result = new LocString("combat_messages", "NOT_ENOUGH_CONDUIT");
        GD.Print("[YgoDuelist NotEnoughConduit] applied NOT_ENOUGH_CONDUIT");
        MainFile.Logger.Info("[YgoDuelist NotEnoughConduit] applied NOT_ENOUGH_CONDUIT");
        return false;
    }
}
