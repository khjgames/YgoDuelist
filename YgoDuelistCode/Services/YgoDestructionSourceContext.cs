using System;
using System.Collections.Generic;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Spell / trap / monster-effect destruction that should respect
/// <see cref="Powers.MagicProtectionKeywordPower"/> and <see cref="Powers.MonsterProtectionKeywordPower"/> on duel pets.
/// Call-sites wrap <see cref="MegaCrit.Sts2.Core.Commands.CreatureCmd.Kill"/> (and related mass destroys) with <see cref="Push"/>.
/// </summary>
public enum YgoDestructionSourceKind
{
    SpellEffect,
    TrapEffect,
    MonsterEffect,
}

public static class YgoDestructionSourceContext
{
    private static readonly Stack<YgoDestructionSourceKind> Stack = new();

    public static YgoDestructionSourceKind? Current => Stack.Count > 0 ? Stack.Peek() : null;

    public static IDisposable Push(YgoDestructionSourceKind kind) => new Scope(kind);

    private sealed class Scope : IDisposable
    {
        public Scope(YgoDestructionSourceKind kind) => Stack.Push(kind);

        public void Dispose()
        {
            if (Stack.Count > 0)
                Stack.Pop();
        }
    }
}
