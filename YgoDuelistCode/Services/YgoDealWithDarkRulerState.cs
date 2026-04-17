using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Cards.Spell.Todo.Normal.A_Deal_with_Dark_Ruler"/> gate (Level 8+ sent to GY this turn) and summon bypass for
/// <see cref="Berserk_Dragon"/> (must not use generic special-summon paths).
/// </summary>
public static class YgoDealWithDarkRulerState
{
    private static readonly Dictionary<ulong, bool> Level8PlusMonsterSentToGraveyardThisTurn = new();

    private static int _dealWithDarkRulerSummonBypassDepth;

    public static bool IsDealWithDarkRulerSummonBypassActive => _dealWithDarkRulerSummonBypassDepth > 0;

    public static void EnterDealWithDarkRulerSummonBypass() => _dealWithDarkRulerSummonBypassDepth++;

    public static void ExitDealWithDarkRulerSummonBypass()
    {
        if (_dealWithDarkRulerSummonBypassDepth > 0)
            _dealWithDarkRulerSummonBypassDepth--;
    }

    public static void RegisterLevel8PlusMonsterSentToGraveyard(Player? player)
    {
        if (player == null)
            return;
        Level8PlusMonsterSentToGraveyardThisTurn[player.NetId] = true;
    }

    public static bool HasLevel8PlusMonsterSentToGraveyardThisTurn(Player? player) =>
        player != null && Level8PlusMonsterSentToGraveyardThisTurn.GetValueOrDefault(player.NetId, false);

    /// <summary>Clears the Level-8+ GY flag for this player; call at the start of their turn.</summary>
    public static void OnPlayerTurnStart(Player? player)
    {
        if (player == null)
            return;
        Level8PlusMonsterSentToGraveyardThisTurn[player.NetId] = false;
    }

}
