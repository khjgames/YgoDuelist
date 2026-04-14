using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch]
public static class CardModelSanganNameLockCanPlayPatch
{
    static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.CanPlay),
            new[] { typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType() })!;

    static void Postfix(CardModel __instance, ref bool __result, ref UnplayableReason reason)
    {
        if (!__result || __instance.Owner is not Player p)
            return;
        if (!YgoSanganNameLock.IsLocked(p, __instance))
            return;

        __result = false;
        reason |= UnplayableReason.BlockedByCardLogic;
    }
}
