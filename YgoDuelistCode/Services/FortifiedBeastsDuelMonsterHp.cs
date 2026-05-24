using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Fortified Beasts bonus is <c>HpFromLevel(effective star level) + total combat bonus</c>.
/// Max/current are synced with <see cref="CreatureCmd.GainMaxHp"/> / <see cref="CreatureCmd.LoseMaxHp"/> so gaining max HP also heals by the same amount (e.g. 4/6 +3 max → 7/9).
/// </summary>
public static class FortifiedBeastsDuelMonsterHp
{
    /// <summary>Base max HP from the card's effective level (same table as <see cref="DuelMonsterModel"/>).</summary>
    public static int GetBaseMaxHpForCard(BaseMonsterCard card) =>
        DuelMonsterModel.HpFromLevel(card.GetEffectiveDuelMonsterLevel());

    /// <summary>Target max HP for this card and player: base from level + total Fortified Beasts bonus this combat.</summary>
    public static int GetTargetMaxHp(BaseMonsterCard card, Player player)
    {
        int bonus = YgoDuelistPassivePowerState.GetFortifiedBeastsTotalBonus(player);
        bonus += card.GetFortifiedBeastsBonusMaxHp(player);
        return GetBaseMaxHpForCard(card) + bonus;
    }

    /// <summary>
    /// Sets this pet's max HP to the calculated target (base + bonus) and adjusts current HP by the max-HP delta.
    /// Call after summon when the pet has its normal level-based max, and after Fortified Beasts (re)applies.
    /// </summary>
    public static async Task SyncPetFromCardAsync(Creature pet, BaseMonsterCard card, Player player)
    {
        if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel || card == null || player == null)
            return;

        int targetMax = GetTargetMaxHp(card, player);
        int delta = targetMax - pet.MaxHp;
        if (delta > 0)
            await CreatureCmd.GainMaxHp(pet, delta);
        else if (delta < 0)
            await CreatureCmd.LoseMaxHp(YgoChoiceContexts.Blocking(), pet, -delta, isFromCard: false);
    }

    /// <summary>
    /// When a monster enters the graveyard or banished pile, re-sync duel pet max HP
    /// (e.g. <see cref="Cards.Monster.Done.Effect.Lava_Golem"/> FIRE count bonuses).
    /// </summary>
    public static void ScheduleSyncAfterMonsterGraveyardOrBanishedPileChanged(CardPile pile, CardModel card)
    {
        if (card is not BaseMonsterCard || !pile.IsCombatPile)
            return;
        if (pile.Type != GraveyardPile.CustomType && pile.Type != BanishedPile.CustomType)
            return;

        Player? player = card.Owner;
        if (player?.Creature?.CombatState == null || player.Creature.Side != CombatSide.Player)
        {
            if (!YgoGraveyardPileHooks.TryGetPlayerForGraveyardAdd(pile, card, out player))
                return;
        }

        TaskHelper.RunSafely(SyncAllPlayerDuelMonstersAsync(player));
    }

    /// <summary>Recompute max/current for every living duel monster the player controls (e.g. after playing Fortified Beasts).</summary>
    public static async Task SyncAllPlayerDuelMonstersAsync(Player player)
    {
        if (player?.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard src)
                continue;
            await SyncPetFromCardAsync(pet, src, player);
        }
    }
}
