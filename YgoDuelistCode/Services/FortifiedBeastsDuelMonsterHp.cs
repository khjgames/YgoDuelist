using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

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
