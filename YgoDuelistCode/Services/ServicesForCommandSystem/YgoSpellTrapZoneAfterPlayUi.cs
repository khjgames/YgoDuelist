using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Deferred cleanup/sync after a card was activated from the Spell/Trap zone (same behavior as the spell/trap zone <c>PlayCardAction</c> patch).
/// Fusion/ritual spells bypass that patch; they call this after <c>OnPlayWrapper</c> when the spell was played from the zone.
/// </summary>
public static class YgoSpellTrapZoneAfterPlayUi
{
    /// <summary>
    /// Delay after synchronous player turn-start state (trap <c>SetThisTurn</c> clear, zone refills, etc.)
    /// before rebuilding the spell/trap second hand so playability reflects fresh energy and card flags.
    /// </summary>
    private const double TurnStartSpellTrapSecondHandRefreshDelaySec = 0.05;

    /// <summary>
    /// Spell/trap zone (and GY) cards use custom <see cref="MegaCrit.Sts2.Core.Entities.Cards.PileType"/> values.
    /// Vanilla <see cref="NCard.FindOnTable"/> only handles hand/play/etc. and throws on unknown pile kinds, which
    /// aborts deferred cleanup and leaves play-queue / second-hand <see cref="NCard"/>s stuck (common in MP).
    /// Same resolution order as option-pile play cleanup (play container, then play queue, then hand/option row).
    /// </summary>
    private static NCard? FindNCardForZonePlayCleanup(CardModel card)
    {
        NCombatUi? ui = NCombatRoom.Instance?.Ui;
        if (ui == null)
            return null;

        NCard? n = ui.GetCardFromPlayContainer(card);
        if (n != null && GodotObject.IsInstanceValid(n))
            return n;

        n = NCardPlayQueue.Instance?.GetCardNode(card);
        if (n != null && GodotObject.IsInstanceValid(n))
            return n;

        n = NPlayerHand.Instance?.GetCard(card);
        return n != null && GodotObject.IsInstanceValid(n) ? n : null;
    }

    public static void ScheduleCleanup(Player? player, CardModel? card)
    {
        if (player == null || card == null)
            return;

        SceneTree? tree = NPlayerHand.Instance?.GetTree();
        if (tree == null)
            return;

        SceneTreeTimer timer = tree.CreateTimer(0.0);
        timer.Timeout += () =>
        {
            NCard? ncard = FindNCardForZonePlayCleanup(card);
            if (ncard != null && GodotObject.IsInstanceValid(ncard))
            {
                ncard.Visible = false;
                if (ncard.GetParent() is NHandCardHolder holder && GodotObject.IsInstanceValid(holder))
                {
                    holder.Visible = false;
                    if (holder.Hitbox != null)
                    {
                        holder.Hitbox.Visible = false;
                        holder.Hitbox.SetEnabled(false);
                    }

                    holder.QueueFree();
                }
                else
                {
                    ncard.QueueFree();
                }
            }

            YgoSecondHandSourceBridge.SetSource(player, YgoSecondHandSource.SpellTrapZone);
            YgoSpellTrapZoneBridge.ForceRefreshSpellTrapSecondHandFromZone(player);
            ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
        };
    }

    /// <summary>
    /// After canceling a blocking selection (fusion/ritual grids) while viewing the spell/trap second hand,
    /// resync the zone list and republish so the row rebuilds and NCards match the pile (same frame as other deferred UI).
    /// </summary>
    public static void ScheduleSecondHandRefreshFromZone(Player? player)
    {
        if (player == null)
            return;

        SceneTree? tree = NPlayerHand.Instance?.GetTree();
        if (tree == null)
            return;

        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        ScheduleSpellTrapSecondHandRepublish(player);
    }

    /// <summary>
    /// When the spell/trap zone panel is the active second-hand source, republish on the next frame so new set cards get holders/NCards.
    /// Call after <see cref="YgoSpellTrapZoneBridge.SyncFromZonePile"/> if the cache is already updated.
    /// </summary>
    public static void ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(Player? player)
    {
        if (player == null || YgoSecondHandSourceBridge.GetSource(player) != YgoSecondHandSource.SpellTrapZone)
            return;
        ScheduleSpellTrapSecondHandRepublish(player);
    }

    /// <summary>
    /// After player turn-start bookkeeping (trap <c>SetThisTurn</c>, refills, etc.), if the spell/trap zone second hand
    /// is visible, resync from the zone pile and republish so costs/playability match the post-turn state.
    /// Runs on a short timer so it executes strictly after synchronous turn-start mutations.
    /// </summary>
    public static void ScheduleSpellTrapSecondHandRefreshAfterTurnStartIfZoneViewActive(Player? player)
    {
        if (player == null)
            return;

        SceneTree? tree = NPlayerHand.Instance?.GetTree();
        if (tree == null)
            return;

        SceneTreeTimer timer = tree.CreateTimer(TurnStartSpellTrapSecondHandRefreshDelaySec);
        timer.Timeout += () =>
        {
            if (YgoSecondHandSourceBridge.GetSource(player) != YgoSecondHandSource.SpellTrapZone)
                return;
            YgoSpellTrapZoneBridge.ForceRefreshSpellTrapSecondHandFromZone(player);
            ScheduleSpellTrapSecondHandRepublish(player);
        };
    }

    /// <summary>
    /// Switches the second hand to the Spell/Trap zone and republishes on the next frame.
    /// Call after <see cref="YgoSpellTrapZoneBridge.SyncFromZonePile"/> when a card enters the zone from the hand (set or continuous play).
    /// </summary>
    public static void ScheduleSpellTrapSecondHandEnsureVisible(Player? player)
    {
        if (player == null)
            return;
        ScheduleSpellTrapSecondHandRepublish(player);
    }

    private static void ScheduleSpellTrapSecondHandRepublish(Player player)
    {
        SceneTree? tree = NPlayerHand.Instance?.GetTree();
        if (tree == null)
            return;

        SceneTreeTimer timer = tree.CreateTimer(0.0);
        timer.Timeout += () =>
            YgoSecondHandSourceBridge.SetSourceAndPublish(player, YgoSecondHandSource.SpellTrapZone);
    }
}
