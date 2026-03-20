using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Summons a duel monster from a monster card into one of 5 zones. Max 5 duel monster summons per player.
/// </summary>
    public static class DuelMonsterSummon
{
    public const int MaxDuelMonstersPerPlayer = 5;

    /// <summary>Scale for duel monster summons (tiny gremlin size) so 5 fit without clutter.</summary>
    public const float DuelMonsterScale = 0.4f;

    /// <summary>
    /// If the player has fewer than 5 duel monster summons, creates a summon from the card's DuelMonsterData
    /// and adds it as a pet. Returns true if a summon was added.
    /// </summary>
    /// <param name="canAttackThisTurn">If true (default), the summon cannot use Command Attack/Defend this turn. If false (e.g. Monster Reborn), they can.</param>
    public static async Task<bool> TrySummonDuelMonster(Player player, BaseMonsterCard card, PlayerChoiceContext _, bool canAttackThisTurn = false)
    {
        if (player?.Creature?.CombatState == null || !card.CanSummonDuelMonster)
            return false;

        int duelMonsterCount = 0;
        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            // Only living duel monsters should occupy a zone.
            if (pet.Monster is DuelMonsterModel && pet.IsAlive)
                duelMonsterCount++;
        }
        if (duelMonsterCount >= MaxDuelMonstersPerPlayer)
            return false;

        DuelMonsterData data = card.GetDuelMonsterData();
        DuelMonsterModel monster = (DuelMonsterModel)ModelDb.Monster<DuelMonsterModel>().ToMutable();
        monster.Level = data.Level;
        monster.SetTitleLoc(data.LocTable, data.LocKey);
        monster.SetPortraitPath(data.PortraitPath);

        Creature petCreature = player.Creature.CombatState.CreateCreature(monster, player.Creature.Side, null);
        player.PlayerCombatState.AddPetInternal(petCreature);
        await CreatureCmd.Add(petCreature);
        // Track this card as an active field monster for aura/stat calculations and menu commands.
        DuelMonsterFieldRegistry.RegisterSummon(player, card, petCreature);

        // Normal summons cannot use Command Attack or Defend this turn; Monster Reborn summons can (caller passes canAttackThisTurn: false).
        if (canAttackThisTurn == false)
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(petCreature, true, player.Creature, card);

        // After the summon completes, move the monster card into the MonsterPile
        // so it is no longer in Hand/Discard/etc.
        await MoveCardToMonsterPile(player, card);

        return true;
    }

    private static async Task MoveCardToMonsterPile(Player player, BaseMonsterCard card)
    {
        if (player.PlayerCombatState == null)
            return;

        CardPile targetPile = MonsterPile.CustomType.GetPile(player);
        if (targetPile == null)
            return;

        // Use CardPileCmd.Add so all standard pile-change hooks and visuals run correctly.
        await CardPileCmd.Add(
            new CardModel[] { card },
            targetPile,
            CardPilePosition.Top,
            card,
            false);
    }

}
