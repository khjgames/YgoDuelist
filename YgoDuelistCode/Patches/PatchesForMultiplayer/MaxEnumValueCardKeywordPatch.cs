using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using Godot;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Vanilla <see cref="MaxEnumValueCache.Get{T}"/> uses only declared <see cref="CardKeyword"/> names (max value 7).
/// <see cref="PacketWriter.WriteEnum"/> / <see cref="PacketReader.ReadEnum"/> choose the bit width from that max.
/// YGO cards use cast values (e.g. 10009, 20051) — with a ~4-bit width those truncate (10009 → 9), so host and client
/// diverge in memory, on the wire, and in MP checksums. Force the reported max high enough that custom keywords
/// round-trip identically on every peer.
/// </summary>
[HarmonyPatch]
public static class MaxEnumValueCardKeywordPatch
{
    /// <summary>Must be &gt;= largest <c>(CardKeyword)</c> literal used in YgoDuelist (currently 20051).</summary>
    public const int YgoMaxCardKeywordValue = 21000;

    private static bool _logged;

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(MaxEnumValueCache), nameof(MaxEnumValueCache.Get))
            .MakeGenericMethod(typeof(CardKeyword));
    }

    [HarmonyPrefix]
    public static bool Prefix(ref int __result)
    {
        int vanillaMax = Enum.GetValues(typeof(CardKeyword)).Cast<int>().Max();
        __result = Math.Max(vanillaMax, YgoMaxCardKeywordValue);
        if (!_logged)
        {
            _logged = true;
            GD.Print(
                $"[YgoDuelist][MP] CardKeyword enum wire width: vanillaMax={vanillaMax} effectiveMax={__result} (YGO custom keywords)");
        }

        return false;
    }
}
