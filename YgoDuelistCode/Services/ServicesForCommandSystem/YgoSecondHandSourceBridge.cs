using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public enum YgoSecondHandSource
{
    MonsterOptions,
    SpellTrapZone
}

/// <summary>
/// Single source-of-truth for which logical list populates the custom second hand.
/// </summary>
public static class YgoSecondHandSourceBridge
{
    private static readonly Dictionary<Player, YgoSecondHandSource> SourceByPlayer = new();

    public static event Action<Player, IReadOnlyList<CardModel>>? SecondHandCardsChanged;

    public static YgoSecondHandSource GetSource(Player player)
    {
        if (SourceByPlayer.TryGetValue(player, out var source))
            return source;
        return YgoSecondHandSource.MonsterOptions;
    }

    public static void SetSource(Player player, YgoSecondHandSource source)
    {
        SourceByPlayer[player] = source;
    }

    public static void SetSourceAndPublish(Player player, YgoSecondHandSource source)
    {
        SetSource(player, source);
        PublishCurrentSourceCards(player);
    }

    public static void NotifyMonsterOptionsChanged(Player player, IReadOnlyList<CardModel> cards)
    {
        if (GetSource(player) != YgoSecondHandSource.MonsterOptions)
            return;
        SecondHandCardsChanged?.Invoke(player, cards);
    }

    public static void NotifySpellTrapZoneChanged(Player player, IReadOnlyList<CardModel> cards)
    {
        if (GetSource(player) != YgoSecondHandSource.SpellTrapZone)
            return;
        SecondHandCardsChanged?.Invoke(player, cards);
    }

    public static void PublishCurrentSourceCards(Player player)
    {
        var cards = GetSource(player) switch
        {
            YgoSecondHandSource.SpellTrapZone => YgoSpellTrapZoneBridge.GetVisibleCards(player),
            _ => YgoOptionHandBridge.GetVisibleOptions(player)
        };
        SecondHandCardsChanged?.Invoke(player, cards);
    }

    /// <summary>
    /// Spell/Trap zone second-hand row is active; switch back to monster options and republish (may be empty).
    /// </summary>
    public static void CloseSpellTrapZoneView(Player player)
    {
        if (GetSource(player) != YgoSecondHandSource.SpellTrapZone)
            return;
        SetSource(player, YgoSecondHandSource.MonsterOptions);
        PublishCurrentSourceCards(player);
    }
}
