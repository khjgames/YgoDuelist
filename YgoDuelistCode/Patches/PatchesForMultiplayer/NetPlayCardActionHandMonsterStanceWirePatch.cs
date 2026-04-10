using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Appends owner hand battle state to <see cref="NetPlayCardAction"/> so observers mirror defense/set, hand-effect,
/// and face-down keyword (10012) — consumed by <see cref="PlayCardActionMpObserverAlignHandMonsterBattleStancePatch"/>.
/// </summary>
[HarmonyPatch(typeof(NetPlayCardAction), nameof(NetPlayCardAction.Serialize))]
public static class NetPlayCardActionHandMonsterStanceSerializePatch
{
    [HarmonyPostfix]
    public static void Postfix(NetPlayCardAction __instance, PacketWriter writer)
    {
        bool atk = true;
        bool he = false;
        bool fd = false;
        bool ws = true;

        CardModel? cm = __instance.card.ToCardModelOrNull();
        if (cm is AbstractMonsterCard amc && cm.Pile?.Type == PileType.Hand)
        {
            atk = amc.IsAttackBattlePosition;
            he = amc.IsHandEffectFormActive;
            fd = amc.FaceDown;
            ws = amc.WillSet;
        }

        writer.WriteBool(atk);
        writer.WriteBool(he);
        writer.WriteBool(fd);
        writer.WriteBool(ws);
    }
}

[HarmonyPatch(typeof(NetPlayCardAction), nameof(NetPlayCardAction.Deserialize))]
public static class NetPlayCardActionHandMonsterStanceDeserializePatch
{
    [HarmonyPostfix]
    public static void Postfix(PacketReader reader, NetPlayCardAction __instance)
    {
        int bitsLeft = reader.Buffer.Length * 8 - reader.BitPosition;
        if (bitsLeft < 4)
        {
            GD.PrintErr($"[YgoDuelist][MP][HandStanceWire] packet missing 4 trailing bits (left={bitsLeft})");
            return;
        }

        bool atk = reader.ReadBool();
        bool he = reader.ReadBool();
        bool fd = reader.ReadBool();
        bool ws = reader.ReadBool();

        CardModel? cm = __instance.card.ToCardModelOrNull();
        if (cm is AbstractMonsterCard && cm.Pile?.Type == PileType.Hand)
            YgoNetPlayCardHandStanceStash.Store(__instance.card.CombatCardIndex, atk, he, fd, ws);
    }
}
