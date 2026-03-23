using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Deferred cleanup/sync after a card was activated from the Spell/Trap zone (same behavior as the spell/trap zone <c>PlayCardAction</c> patch).
/// Fusion/ritual spells bypass that patch; they call this after <c>OnPlayWrapper</c> when the spell was played from the zone.
/// </summary>
public static class YgoSpellTrapZoneAfterPlayUi
{
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
            NCard? ncard = NCard.FindOnTable(card);
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
            YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        };
    }
}
