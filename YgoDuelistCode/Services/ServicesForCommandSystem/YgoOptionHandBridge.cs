using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Lightweight bridge between the logical YgoDuelist card option pile and any UI
/// implementation that wants to present those options as a "second hand".
///
/// This class deliberately does NOT create or manage NCards / NCardHolders
/// directly. Instead it:
/// - Tracks which option cards are currently "visible" for a given player.
/// - Raises an event whenever that logical set changes.
///
/// Godot-side code (or other C# nodes that own actual NCards) are expected
/// to subscribe to <see cref="OptionsChanged"/> and build / destroy
/// visual card rows, wire up targeting, etc. This keeps all of the
/// base-game integration in one place and avoids abusing CustomPile
/// visibility hooks, per this mod's requirements.
/// </summary>
public static class YgoOptionHandBridge
{
    /// <summary>
    /// Logical snapshot of which option cards should currently be shown
    /// for each combat player. Limited to a small, fixed-size "hand".
    /// </summary>
    private static readonly Dictionary<Player, List<CardModel>> _visibleOptions = new();

    /// <summary>
    /// Fired whenever the logical option "hand" for a player changes.
    /// Subscribers are expected to own all NCards / NCardHolders and
    /// rebuild their visuals to match the provided list.
    /// </summary>
    public static event Action<Player, IReadOnlyList<CardModel>>? OptionsChanged;

    /// <summary>
    /// Sort key for option command cards so second hand order is:
    /// Defend, Attack, <see cref="Activate_Effect"/> (and other <see cref="MonsterCommandCard"/> extras), Change position, Toggle Die, Exit.
    /// Public so callers can insert cards at the correct index when modifying the pile.
    /// </summary>
    public static int GetOptionCardSortKey(CardModel c)
    {
        return c switch
        {
            Command_Defend => 0,
            Command_Attack => 1,
            Activate_Effect => 2,
            Command_Change_Battle_Position => 3,
            Toggle_Die_For_You => 4,
            Exit_Monster_Options => 5,
            MonsterCommandCard => 2,
            _ => 6
        };
    }

    private static int OptionCardSortKey(CardModel c) => GetOptionCardSortKey(c);

    /// <summary>
    /// Runs <see cref="SyncFromOptionPile"/> after the current frame so the option-hand UI rebuild happens when
    /// <see cref="NPlayerHand.InCardPlay"/> is false. Calling <see cref="SyncFromOptionPile"/> synchronously from
    /// unplayable option handlers (while the clicked holder is still the active play holder) prevents
    /// the option-hand UI rebuild from removing that holder cleanly and leaves extra
    /// <c>NYgoOptionCardHolder</c> instances in <see cref="NPlayerHand.ActiveHolders"/>.
    /// </summary>
    public static void RequestDeferredSyncFromOptionPile(Player player)
    {
        if (player == null)
            return;
        var tree = NPlayerHand.Instance?.GetTree();
        if (tree == null)
            return;
        var p = player;
        var timer = tree.CreateTimer(0.0);
        timer.Timeout += () => SyncFromOptionPile(p);
    }

    /// <summary>
    /// Returns the current logical option cards for the given player.
    /// This is purely a data view; visuals are owned by subscribers.
    /// </summary>
    public static IReadOnlyList<CardModel> GetVisibleOptions(Player player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        return _visibleOptions.TryGetValue(player, out var list)
            ? list
            : Array.Empty<CardModel>();
    }

    /// <summary>
    /// Synchronise the logical option "hand" from the player's
    /// <see cref="YgoCardOptionPile"/> contents, clamped to maxCount.
    ///
    /// This is intended to be called whenever the option pile is rebuilt
    /// (e.g., on duel monster right-click, or after an option is played).
    /// </summary>
    /// <param name="player">The combat player whose option pile to read.</param>
    /// <param name="maxCount">
    /// Maximum number of options to expose as a second hand. Cards beyond
    /// this limit remain in the pile but are not part of the logical hand.
    /// </param>
    public static void SyncFromOptionPile(Player player, int maxCount = 7)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        var pile = YgoCardOptionPile.CustomType.GetPile(player);
        if (pile == null)
        {
            // No option pile: clear any existing state and notify listeners.
            bool hadState = _visibleOptions.Remove(player);
            if (hadState)
            {
                var empty = Array.Empty<CardModel>();
                OptionsChanged?.Invoke(player, empty);
                YgoSecondHandSourceBridge.NotifyMonsterOptionsChanged(player, empty);
            }
            return;
        }

        // Sort first so priority slots aren't eaten by pile insertion / async ordering, then clamp.
        List<CardModel> cards = pile.Cards
            .OrderBy(OptionCardSortKey)
            .Take(Math.Max(0, maxCount))
            .ToList();

        _visibleOptions[player] = cards;
        OptionsChanged?.Invoke(player, cards);
        YgoSecondHandSourceBridge.NotifyMonsterOptionsChanged(player, cards);
    }

    /// <summary>
    /// Clears all logical option state for the given player and notifies
    /// subscribers that the second-hand zone is now empty.
    /// </summary>
    public static void Clear(Player player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        bool removed = _visibleOptions.Remove(player);
        if (removed)
        {
            var empty = Array.Empty<CardModel>();
            OptionsChanged?.Invoke(player, empty);
            YgoSecondHandSourceBridge.NotifyMonsterOptionsChanged(player, empty);
        }
    }
}
