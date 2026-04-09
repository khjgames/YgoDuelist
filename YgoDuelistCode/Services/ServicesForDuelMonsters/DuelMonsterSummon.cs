using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Summons a duel monster from a monster card into one of 5 zones. Max 5 duel monster summons per player.
/// </summary>
    public static class DuelMonsterSummon
{
    public const int MaxDuelMonstersPerPlayer = 5;

    /// <summary>Scale for duel monster summons (tiny gremlin size) so 5 fit without clutter.</summary>
    public const float DuelMonsterScale = 0.4f;

    /// <summary>Living <see cref="DuelMonsterModel"/> pets for this player (same rule as <see cref="TrySummonDuelMonster"/>).</summary>
    public static int CountLiveDuelMonsters(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return 0;

        int n = 0;
        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            if (pet.Monster is DuelMonsterModel && pet.IsAlive)
                n++;
        }

        return n;
    }

    /// <summary>
    /// True if, after this play releases <paramref name="tributeReleaseCount"/> field monsters then summons one,
    /// the total would not exceed <see cref="MaxDuelMonstersPerPlayer"/>.
    /// </summary>
    public static bool HasRoomForDuelSummonAfterReleasing(Player? player, int tributeReleaseCount)
    {
        if (player?.PlayerCombatState == null)
            return false;

        int live = CountLiveDuelMonsters(player);
        int after = live - tributeReleaseCount + 1;
        return after <= MaxDuelMonstersPerPlayer;
    }

    /// <summary>
    /// Ritual, fusion, Monster Reborn, and similar: the pet is not marked as having used Command this turn, so Attack/Defend are allowed.
    /// </summary>
    public static Task<bool> TrySummonDuelMonsterSpecial(Player player, BaseMonsterCard card, PlayerChoiceContext ctx) =>
        TrySummonDuelMonster(player, card, ctx, canAttackThisTurn: true);

    /// <summary>
    /// If the player has fewer than 5 duel monster summons, creates a summon from the card's DuelMonsterData
    /// and adds it as a pet. Returns true if a summon was added.
    /// </summary>
    /// <param name="canAttackThisTurn">If <c>true</c>, Command Attack/Defend may be used this turn. If <c>false</c> (normal/tribute default), they are exhausted (stiff/fatigued) this turn.</param>
    public static async Task<bool> TrySummonDuelMonster(Player player, BaseMonsterCard card, PlayerChoiceContext _, bool canAttackThisTurn = false)
    {
        if (player?.Creature?.CombatState == null
            || (!card.CanSummonDuelMonster && !card.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate))
            return false;

        if (CountLiveDuelMonsters(player) >= MaxDuelMonstersPerPlayer)
            return false;

        int legionCountBeforeSummon = LegionFiendJesterSpellcasterConduit.CountLegionsOnField(player);

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

        if (card is Hourglass_of_Courage && !canAttackThisTurn)
            await PowerCmd.Apply<HourglassOfCourageHalvedPower>(petCreature, 2m, player.Creature, card);

        bool stumblingField = YgoStumblingField.IsActive(player);
        if (stumblingField)
            await PowerCmd.Apply<YgoStumblingDefendOnlyPower>(petCreature, 1m, player.Creature, null);

        bool anubisTurn = player.Creature.HasPower<YgoCurseOfAnubisPlayerMarkerPower>();
        if (anubisTurn && card is EffectMonsterCard && !petCreature.HasPower<YgoCurseOfAnubisEffectMonsterPower>())
            await PowerCmd.Apply<YgoCurseOfAnubisEffectMonsterPower>(petCreature, 1m, player.Creature, null);

        await LimiterRemovalPower.OnMachineDuelMonsterSummonedAsync(player, petCreature, card);

        // Normal/tribute summons: mark Command as used this turn. Special summons pass canAttackThisTurn: true.
        // Stumbling: summons may still Command Defend; YgoStumblingDefendOnlyPower blocks Attack only.
        bool skipStiff = card is BaseMonsterCard bm && bm.NormalSummonSkipsStiffFatigueOnSummonTurn;
        if (!canAttackThisTurn && !stumblingField && !skipStiff)
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(petCreature, true, player.Creature, card);

        await DuelMonsterStancePowerSync.SyncForPetAsync(petCreature, card, player.Creature, card);

        if (card is The_Hunter_with_7_Weapons hunterCard)
        {
            GraveyardRelic? gy = player.Relics.OfType<GraveyardRelic>().FirstOrDefault();
            gy?.OnHunterSummonedDuringSevenWeaponsBonus(hunterCard);
            await GraveyardRelic.SyncSevenWeaponsCounterPetsAsync(player);
        }

        if (card is Cure_Mermaid cureMermaid)
        {
            await MonsterCommandRegistry.SetDieForYouForcedAsync(petCreature, true, player, cureMermaid);
            NCombatRoom.Instance?.GetCreatureNode(petCreature)?.TrackBlockStatus(player.Creature);
        }

        // After the summon completes, move the monster card into the MonsterPile
        // so it is no longer in Hand/Discard/etc.
        await MoveCardToMonsterPile(player, card);

        if (player.Creature?.GetPower<ChainSummoningPower>() is { } chain)
        {
            int every = (int)chain.Amount;
            if (every > 0)
            {
                int stars = YgoDuelistPassivePowerState.RegisterChainSummoningSummon(player, every);
                for (int i = 0; i < stars; i++)
                    await PlayerCmd.GainStars(1, player);
            }
        }

        await FortifiedBeastsDuelMonsterHp.SyncPetFromCardAsync(petCreature, card, player);

        LegionFiendJesterSpellcasterConduit.RegisterWaivedSummonAfterNormalSpellcasterSummon(
            player,
            card,
            canAttackThisTurn,
            legionCountBeforeSummon);

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
