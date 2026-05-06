using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// MP + Cost Down: <see cref="MegaCrit.Sts2.Core.Entities.Cards.BaseMonsterCard.GetEffectiveDuelMonsterLevel"/> only applies
/// hand Cost Down while the card is in <see cref="MegaCrit.Sts2.Core.Entities.Cards.PileType.Hand"/>. By <see cref="YgoDuelist.YgoDuelistCode.Cards.Core.NormalMonsterCard.OnPlay"/>,
/// the card may already be off-hand, so <see cref="AbstractMonsterCard.TributeReleaseCount"/> can jump (e.g. Labyrinth Wall
/// effective 3 → printed 5). Lock the tribute count computed while still in hand, keyed like <see cref="TributeSummonPlayPayload"/>.
/// </summary>
public static class NormalSummonHandTributeNeedLock
{
    private static readonly System.Collections.Generic.Dictionary<(ulong OwnerNetId, uint CombatCardIndex), int> Pending = new();
    private static readonly object Gate = new();

    public static void Set(ulong ownerNetId, uint combatCardIndex, int requiredTributeCount)
    {
        lock (Gate)
            Pending[(ownerNetId, combatCardIndex)] = requiredTributeCount;
    }

    public static bool TryTake(ulong ownerNetId, uint combatCardIndex, out int requiredTributeCount)
    {
        lock (Gate)
        {
            if (!Pending.TryGetValue((ownerNetId, combatCardIndex), out requiredTributeCount))
                return false;
            Pending.Remove((ownerNetId, combatCardIndex));
            return true;
        }
    }

    /// <summary>Remove and return stashed need if present; otherwise returns false (usual for non–hand plays).</summary>
    public static bool TryConsume(PlayerChoiceContext? choiceContext, CardModel card, out int lockedTributeNeed)
    {
        lockedTributeNeed = 0;
        if (choiceContext is GameActionPlayerChoiceContext { Action: PlayCardAction pca })
            return TryTake(pca.Player.NetId, pca.NetCombatCard.CombatCardIndex, out lockedTributeNeed);

        return YgoPlayPayloadNetKey.TryGetKey(card, out ulong oid, out uint idx)
               && TryTake(oid, idx, out lockedTributeNeed);
    }

    public static void ClearForKey(ulong ownerNetId, uint combatCardIndex)
    {
        lock (Gate)
            Pending.Remove((ownerNetId, combatCardIndex));
    }
}
