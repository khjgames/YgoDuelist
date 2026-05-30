using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Big Deck / Tiny Deck badges use <see cref="SerializablePlayer.Deck"/>.Count, which for YgoDuelist includes extra/trunk/side trailer cards.
/// </summary>
[HarmonyPatch]
public static class YgoBadgeDeckCountPatch
{
    private static readonly FieldInfo LocalPlayerField = AccessTools.Field(typeof(Badge), "_localPlayer")!;

    [HarmonyPatch(typeof(BigDeck), nameof(BigDeck.Rarity), MethodType.Getter)]
    [HarmonyPostfix]
    public static void BigDeckRarityPostfix(BigDeck __instance, ref BadgeRarity __result)
    {
        if (!TryGetMainDeckCount(__instance, out int count))
            return;

        if (count >= 60)
            __result = count >= 100 ? BadgeRarity.Gold : BadgeRarity.Silver;
        else if (count >= 40)
            __result = BadgeRarity.Bronze;
        else
            __result = BadgeRarity.None;
    }

    [HarmonyPatch(typeof(TinyDeck), nameof(TinyDeck.Rarity), MethodType.Getter)]
    [HarmonyPostfix]
    public static void TinyDeckRarityPostfix(TinyDeck __instance, ref BadgeRarity __result)
    {
        if (!TryGetMainDeckCount(__instance, out int count))
            return;

        if (count <= 10)
            __result = count <= 5 ? BadgeRarity.Gold : BadgeRarity.Silver;
        else if (count <= 20)
            __result = BadgeRarity.Bronze;
        else
            __result = BadgeRarity.None;
    }

    private static bool TryGetMainDeckCount(Badge badge, out int count)
    {
        count = 0;
        var player = (SerializablePlayer)LocalPlayerField.GetValue(badge)!;
        if (player.CharacterId == null || !YgoSerializableDeckLists.IsYgoCharacter(player.CharacterId))
            return false;

        count = YgoSerializableDeckLists.MainDeckForCharacter(player.CharacterId, player.Deck).Count;
        return true;
    }
}
