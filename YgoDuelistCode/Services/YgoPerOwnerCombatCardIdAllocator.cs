using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Godot;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Per-owner combat card ids: <c>id = BandStride * playerSlot + localIndex</c>. Vanilla MP packets carry the combat card
/// index in <b>16 bits</b>; values must stay in <c>0..65535</c> or peers truncate and resolve the wrong <see cref="CardModel"/>.
/// <para>
/// Layout: run roster slots <c>0..MaxPlayerSlot</c> each get <see cref="BandStride"/> distinct locals (2048 cards per player).
/// Slot <see cref="OrphanSlotIndex"/> is reserved for cards without a mapped owner (same reconstruction rule on every peer:
/// full id = <c>BandStride * 31 + local</c>).
/// </para>
/// </summary>
public static class YgoPerOwnerCombatCardIdAllocator
{
    /// <summary>Bits of local index per player band; 2048 × 32 slots = 65536 values → max wire-safe id 65535.</summary>
    public const uint BandStride = 2048u;

    /// <summary>Slots <c>0..MaxPlayerSlot</c> map from <see cref="IPlayerCollection.GetPlayerSlotIndex"/> (clamped).</summary>
    public const int MaxPlayerSlot = 30;

    /// <summary>Band index used only for ownerless / unmapped-owner ids (63488..65535).</summary>
    public const int OrphanSlotIndex = 31;

    /// <summary>Wire-safe maximum (inclusive) for <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.NetCombatCard.CombatCardIndex"/>.</summary>
    public const uint MaxWireCombatCardIndex = 65535u;

    private static readonly Dictionary<ulong, int> NetIdToSlot = new();

    private static readonly Dictionary<ulong, uint> NextLocalByOwnerNetId = new();

    private static uint _orphanLocal;

    /// <summary>
    /// Call at the start of <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.NetCombatCardDb.StartCombat"/> before any
    /// card is id'd. Maps each <see cref="Player.NetId"/> to
    /// <c>player.RunState.GetPlayerSlotIndex(player)</c> (host/run roster order from save / lobby).
    /// </summary>
    public static void PrepareCombat(IReadOnlyList<Player>? players)
    {
        NetIdToSlot.Clear();
        NextLocalByOwnerNetId.Clear();
        _orphanLocal = 0u;

        if (players == null || players.Count == 0)
            return;

        int fallbackSlot = 0;
        foreach (Player p in players)
        {
            int slot = p.RunState.GetPlayerSlotIndex(p);
            if (slot < 0)
                slot = fallbackSlot++;

            if (slot > MaxPlayerSlot)
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP] GetPlayerSlotIndex={slot} exceeds MaxPlayerSlot={MaxPlayerSlot}; clamping (risk of id pressure).");
                slot = MaxPlayerSlot;
            }

            NetIdToSlot[p.NetId] = slot;
            NextLocalByOwnerNetId[p.NetId] = 0u;
        }
    }

    /// <summary>
    /// After the combat id maps are cleared (e.g. YGO deterministic re-id postfix), reset only the per-owner
    /// sequences so re-assignment starts at <c>BandStride * slot + 0</c> for each owner.
    /// </summary>
    public static void ResetSequenceCountersOnly()
    {
        _orphanLocal = 0u;
        ulong[] keys = NextLocalByOwnerNetId.Keys.ToArray();
        foreach (ulong k in keys)
            NextLocalByOwnerNetId[k] = 0u;
    }

    /// <summary>Allocates the next uint for this card; caller must insert into NetCombatCardDb maps. Stays ≤ <see cref="MaxWireCombatCardIndex"/>.</summary>
    public static uint AllocateNextId(CardModel card)
    {
        if (card.Owner == null)
            return AllocateOrphanId();

        ulong ownerNet = card.Owner.NetId;
        if (!NetIdToSlot.TryGetValue(ownerNet, out int slot))
            return AllocateOrphanId();

        NextLocalByOwnerNetId.TryGetValue(ownerNet, out uint local);
        if (local >= BandStride)
            throw new InvalidOperationException(
                $"YgoPerOwnerCombatCardIdAllocator: per-owner band full (>={BandStride} cards); ownerNet={ownerNet} slot={slot}.");

        uint id = checked(BandStride * (uint)slot + local);
        NextLocalByOwnerNetId[ownerNet] = local + 1u;
        return id;
    }

    /// <summary>Reconstructs the same uint every peer uses: <c>BandStride * slot + local</c> (see class summary).</summary>
    public static uint ReconstructFullIndex(int rosterSlot, uint localIndex)
    {
        int slot = rosterSlot;
        if (slot < 0)
            slot = 0;
        if (slot > MaxPlayerSlot)
            slot = MaxPlayerSlot;
        return checked(BandStride * (uint)slot + localIndex);
    }

    private static uint AllocateOrphanId()
    {
        if (_orphanLocal >= BandStride)
            throw new InvalidOperationException("YgoPerOwnerCombatCardIdAllocator: orphan band (slot 31) exhausted.");

        uint id = checked(BandStride * (uint)OrphanSlotIndex + _orphanLocal);
        _orphanLocal++;
        return id;
    }
}
