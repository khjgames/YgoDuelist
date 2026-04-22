using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
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

        // Authoritative enqueue-time spend snapshot for observer mirror: this preserves temporary
        // cost modifiers (e.g. potion/relic/hook) even when observer local state differs.
        int energyToSpend = 0;
        int starsToSpend = 0;
        if (cm != null)
        {
            Player? owner = cm.Owner;
            int ownerEnergy = owner?.PlayerCombatState?.Energy ?? 0;
            energyToSpend = cm.EnergyCost.GetAmountToSpend();
            starsToSpend = System.Math.Max(0, cm.GetStarCostWithModifiers());
            if (energyToSpend > ownerEnergy)
            {
                CombatState? combatState = cm.CombatState;
                if (owner == null || combatState == null)
                {
                    GD.Print(
                        $"[YgoDuelist][MP][HandStanceWire] excess-energy snapshot skipped (no hook): ownerNull={owner == null} combatStateNull={combatState == null} card={cm.Id?.Entry}");
                }
                else if (Hook.ShouldPayExcessEnergyCostWithStars(combatState, owner))
                {
                    starsToSpend += (energyToSpend - ownerEnergy) * 2;
                    energyToSpend = ownerEnergy;
                }
            }
        }

        writer.WriteBool(atk);
        writer.WriteBool(he);
        writer.WriteBool(fd);
        writer.WriteBool(ws);
        writer.WriteInt(energyToSpend);
        writer.WriteInt(starsToSpend);
    }
}

[HarmonyPatch(typeof(NetPlayCardAction), nameof(NetPlayCardAction.Deserialize))]
public static class NetPlayCardActionHandMonsterStanceDeserializePatch
{
    [HarmonyPostfix]
    public static void Postfix(PacketReader reader, NetPlayCardAction __instance)
    {
        int bitsLeft = reader.Buffer.Length * 8 - reader.BitPosition;
        if (bitsLeft < 68)
        {
            GD.PrintErr($"[YgoDuelist][MP][HandStanceWire] packet missing trailing stance/cost bits (left={bitsLeft})");
            return;
        }

        bool atk = reader.ReadBool();
        bool he = reader.ReadBool();
        bool fd = reader.ReadBool();
        bool ws = reader.ReadBool();
        int energyToSpend = reader.ReadInt();
        int starsToSpend = reader.ReadInt();

        CardModel? cm = __instance.card.ToCardModelOrNull();
        if (cm is AbstractMonsterCard && cm.Pile?.Type == PileType.Hand)
        {
            YgoNetPlayCardHandStanceStash.Store(__instance.card.CombatCardIndex, atk, he, fd, ws);
            YgoNetPlayCardResourceSpendStash.Store(__instance.card.CombatCardIndex, energyToSpend, starsToSpend);
        }
    }
}
