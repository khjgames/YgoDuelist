using System.Collections.Generic;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// While <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardModel.OnSummoned"/> runs, indicates whether the summon was
/// normal/tribute style (<c>true</c>) or special (<c>false</c>). See <see cref="DuelMonsterSummon.TrySummonDuelMonster"/>.
/// </summary>
public static class YgoDuelMonsterSummonStyleContext
{
    private static readonly Stack<bool> Stack = new();

    public static void Push(bool isNormalOrTributeSummon) => Stack.Push(isNormalOrTributeSummon);

    public static void Pop()
    {
        if (Stack.Count > 0)
            Stack.Pop();
    }

    /// <summary>When outside summon flow, returns <c>null</c>.</summary>
    public static bool? CurrentNormalOrTribute => Stack.Count == 0 ? null : Stack.Peek();
}
