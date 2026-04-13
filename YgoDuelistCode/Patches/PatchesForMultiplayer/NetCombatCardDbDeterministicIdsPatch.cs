using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;
using Godot;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// <see cref="NetCombatCardDb.StartCombat"/> assigns each mutable combat <see cref="CardModel"/> a sequential id used in
/// <see cref="NetCombatCard"/>. Vanilla iterates <c>players</c> in run list order; host vs client can use different orders,
/// so the same uint index can refer to different card instances. <see cref="PlayCardAction"/> then resolves the wrong
/// card or returns early when the pile is not Hand — a common source of MP desync and checksum mismatch.
/// After vanilla runs, clear the maps and re-assign ids in a deterministic order: players by run roster slot
/// (<see cref="MegaCrit.Sts2.Core.Runs.IPlayerCollection.GetPlayerSlotIndex(MegaCrit.Sts2.Core.Entities.Players.Player)"/> —
/// same order as the hosted run’s player list, not raw NetId order), then each pile in
/// <see cref="PlayerCombatState.AllPiles"/> order, then each YGO custom combat pile in a fixed order
/// (matches checksum snapshot), then each card in pile list order. Actual uint values use
/// <see cref="YgoPerOwnerCombatCardIdAllocator"/> (per-owner bands); this postfix still ensures the same visitation order on every peer.
/// <para>
/// Do <b>not</b> clear and re-assign all ids again after combat has started (e.g. on every <c>IdCardIfNecessary</c>).
/// <see cref="NetCombatCard"/> values are embedded in networked <c>PlayCardAction</c> and other messages; reshuffling
/// uint→card mappings mid-combat makes the same id refer to different <see cref="CardModel"/> instances on host vs
/// client (classic desync: one peer plays Kattapillar, the other resolves the same id to Strike).
/// </para>
/// </summary>
[HarmonyPatch(typeof(NetCombatCardDb), nameof(NetCombatCardDb.StartCombat))]
public static class NetCombatCardDbDeterministicIdsPatch
{
    private static readonly MethodInfo IdCardIfNecessary = AccessTools.Method(typeof(NetCombatCardDb), "IdCardIfNecessary");

    private static bool _loggedOnce;

    [HarmonyPostfix]
    public static void Postfix(NetCombatCardDb __instance, IReadOnlyList<Player> players)
    {
        if (players == null || players.Count == 0 || IdCardIfNecessary == null)
            return;

        Traverse tr = Traverse.Create(__instance);
        Dictionary<uint, CardModel> idToCard = tr.Field<Dictionary<uint, CardModel>>("_idToCard").Value;
        Dictionary<CardModel, uint> cardToId = tr.Field<Dictionary<CardModel, uint>>("_cardToId").Value;

        idToCard.Clear();
        cardToId.Clear();
        tr.Field<uint>("_nextId").Value = 0u;

        YgoPerOwnerCombatCardIdAllocator.ResetSequenceCountersOnly();

        foreach (Player player in players
                     .OrderBy(p => p.RunState.GetPlayerSlotIndex(p))
                     .ThenBy(p => p.NetId))
        {
            if (player.PlayerCombatState == null)
                continue;

            foreach (CardPile pile in player.PlayerCombatState.AllPiles)
            {
                foreach (CardModel card in pile.Cards)
                    IdCardIfNecessary.Invoke(__instance, new object[] { card });
            }

            IdCardYgoCustomCombatPiles(__instance, player);
        }

        if (!_loggedOnce)
        {
            _loggedOnce = true;
            GD.Print($"[YgoDuelist][MP] NetCombatCardDb: deterministic combat card IDs (count={idToCard.Count})");
        }
    }

    /// <summary>Same pile order as <c>NetFullCombatStateYgoChecksumPatch.AppendYgoCustomCombatPiles</c>.</summary>
    private static void IdCardYgoCustomCombatPiles(NetCombatCardDb db, Player player)
    {
        if (IdCardIfNecessary == null)
            return;

        TryIdCardPile(db, YgoCardOptionPile.CustomType.GetPile(player));
        TryIdCardPile(db, SpellTrapZonePile.CustomType.GetPile(player));
        TryIdCardPile(db, GraveyardPile.CustomType.GetPile(player));
        TryIdCardPile(db, MonsterPile.CustomType.GetPile(player));
        TryIdCardPile(db, FieldPile.CustomType.GetPile(player));
        TryIdCardPile(db, ExtraDeckPile.CustomType.GetPile(player));
        TryIdCardPile(db, BanishedPile.CustomType.GetPile(player));
    }

    private static void TryIdCardPile(NetCombatCardDb db, CardPile? pile)
    {
        if (pile == null)
            return;
        foreach (CardModel card in pile.Cards)
            IdCardIfNecessary.Invoke(db, new object[] { card });
    }
}
