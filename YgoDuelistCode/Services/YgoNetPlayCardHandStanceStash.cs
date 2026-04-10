using System.Collections.Generic;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Owner hand battle state from trailing <see cref="MegaCrit.Sts2.Core.GameActions.NetPlayCardAction"/> bits;
/// consumed in <see cref="MegaCrit.Sts2.Core.GameActions.PlayCardAction.ExecuteAction"/> on observers.
/// </summary>
public static class YgoNetPlayCardHandStanceStash
{
    private static readonly Dictionary<uint, (bool Attack, bool HandEffect, bool FaceDown, bool WillSet)> Pending = new();
    private static readonly object Gate = new();

    public static void Store(uint combatCardIndex, bool attack, bool handEffect, bool faceDown, bool willSet)
    {
        lock (Gate)
            Pending[combatCardIndex] = (attack, handEffect, faceDown, willSet);
    }

    public static bool TryTake(uint combatCardIndex, out bool attack, out bool handEffect, out bool faceDown, out bool willSet)
    {
        lock (Gate)
        {
            if (Pending.TryGetValue(combatCardIndex, out var t))
            {
                Pending.Remove(combatCardIndex);
                attack = t.Attack;
                handEffect = t.HandEffect;
                faceDown = t.FaceDown;
                willSet = t.WillSet;
                return true;
            }
        }

        attack = false;
        handEffect = false;
        faceDown = false;
        willSet = true;
        return false;
    }
}
