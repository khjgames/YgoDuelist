using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Cards.Monster.Done.Effect.Reactor_Slime"/> effect 1: after resolution, for the rest of the turn only Divine-Beast
/// monsters may be normal or special summoned. Also: cannot activate effect 1 if the player already normal/special summoned
/// any non–Divine-Beast monster this turn (including this card's own normal summon). State is keyed by
/// <see cref="Player.NetId"/> so host/client agree (no <see cref="Player"/> reference equality drift).
/// </summary>
public static class ReactorSlimeSummonGate
{
    private static readonly HashSet<ulong> RestrictedNetIds = new();
    private static readonly HashSet<ulong> NonDivineSummonThisTurnNetIds = new();

    public static void MarkRestricted(Player? player)
    {
        if (player != null)
            RestrictedNetIds.Add(player.NetId);
    }

    public static bool BlocksNonDivineSummons(Player? player) =>
        player != null && RestrictedNetIds.Contains(player.NetId);

    public static bool AllowsSummon(Player? player, BaseMonsterCard? card)
    {
        if (!BlocksNonDivineSummons(player))
            return true;
        return card != null && card.GetEffectiveDuelMonsterRace() == DuelMonsterRace.DivineBeast;
    }

    /// <summary>
    /// Playability / preview when there is no live <see cref="BaseMonsterCard"/> yet (e.g. token spells before combat creates the token instance).
    /// Approximates <see cref="AllowsSummon"/> using printed race (tokens rarely override effective race).
    /// </summary>
    public static bool AllowsSummonPrintedRace(Player? player, DuelMonsterRace printedRace) =>
        !BlocksNonDivineSummons(player) || printedRace == DuelMonsterRace.DivineBeast;

    /// <summary>Compose after rule-specific filters in LINQ so grids mirror <see cref="DuelMonsterSummon.TrySummonDuelMonster"/>.</summary>
    public static Func<TMonster, bool> SummonCandidatePredicate<TMonster>(Player? player)
        where TMonster : BaseMonsterCard =>
        m => AllowsSummon(player, m);

    /// <summary>
    /// True after any non–Divine-Beast duel monster was successfully summoned this turn (normal or special).
    /// Used to gate Reactor Slime activated effect 1 — cannot be activated the turn you summoned this monster, etc.
    /// </summary>
    public static bool HasSummonedNonDivineMonsterThisTurn(Player? player) =>
        player != null && NonDivineSummonThisTurnNetIds.Contains(player.NetId);

    public static void RecordSummon(Player? player, BaseMonsterCard? card)
    {
        if (player == null || card == null)
            return;
        if (card.GetEffectiveDuelMonsterRace() == DuelMonsterRace.DivineBeast)
            return;
        NonDivineSummonThisTurnNetIds.Add(player.NetId);
    }

    public static void ResetForPlayer(Player? player)
    {
        if (player == null)
            return;
        RestrictedNetIds.Remove(player.NetId);
        NonDivineSummonThisTurnNetIds.Remove(player.NetId);
    }

    public static void ClearAll()
    {
        RestrictedNetIds.Clear();
        NonDivineSummonThisTurnNetIds.Clear();
    }
}
