using MegaCrit.Sts2.Core.Combat;
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
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
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
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
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
        TrySummonDuelMonster(player, card, ctx, canAttackThisTurn: true, isSpecialSummonRoute: true);

    /// <summary>
    /// In co-op, <see cref="Creature.CombatState"/> on a remote peer’s <see cref="Player.Creature"/> can be unset while
    /// that player’s duel pets still hold the live <see cref="CombatState"/>. Summon must use that state so option-pile
    /// special summons (e.g. union fusion) do not fail after materials are already gone on whichever client runs first.
    /// </summary>
    private static CombatState? ResolveCombatStateForPlayerSummon(Player player)
    {
        CombatState? cs = player.Creature?.CombatState;
        if (cs != null)
            return cs;

        if (player.PlayerCombatState != null)
        {
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (pet?.CombatState != null)
                    return pet.CombatState;
            }
        }

        return CombatManager.Instance?.DebugOnlyGetState();
    }

    /// <summary>
    /// If the player has fewer than 5 duel monster summons, creates a summon from the card's DuelMonsterData
    /// and adds it as a pet. Returns true if a summon was added.
    /// </summary>
    /// <param name="canAttackThisTurn">If <c>true</c>, Command Attack/Defend may be used this turn. If <c>false</c> (normal/tribute default), they are exhausted (stiff/fatigued) this turn.</param>
    public static async Task<bool> TrySummonDuelMonster(
        Player player,
        BaseMonsterCard card,
        PlayerChoiceContext _,
        bool canAttackThisTurn = false,
        bool isSpecialSummonRoute = false)
    {
        if (player?.Creature == null
            || player.PlayerCombatState == null
            || (!card.CanSummonDuelMonster && !card.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate))
            return false;

        if (isSpecialSummonRoute && card.BlocksSpecialDuelMonsterSummon)
            return false;

        CombatState? combatState = ResolveCombatStateForPlayerSummon(player);
        if (combatState == null)
            return false;

        if (!ReactorSlimeSummonGate.AllowsSummon(player, card))
            return false;

        if (!YgoFushiohRichieSummonGate.AllowsSpecialSummon(player, card))
            return false;

        if (CountLiveDuelMonsters(player) >= MaxDuelMonstersPerPlayer)
            return false;

        int legionCountBeforeSummon = LegionFiendJesterSpellcasterConduit.CountLegionsOnField(player);

        DuelMonsterData data = card.GetDuelMonsterData();
        DuelMonsterModel monster = (DuelMonsterModel)ModelDb.Monster<DuelMonsterModel>().ToMutable();
        monster.Level = data.Level;
        monster.SetTitleLoc(data.LocTable, data.LocKey);
        monster.SetPortraitPath(data.PortraitPath);

        Creature petCreature = combatState.CreateCreature(monster, player.Creature.Side, null);
        player.PlayerCombatState.AddPetInternal(petCreature);
        await CreatureCmd.Add(petCreature);
        // Track this card as an active field monster for aura/stat calculations and menu commands.
        DuelMonsterFieldRegistry.RegisterSummon(player, card, petCreature);

        YgoDuelMonsterSummonStyleContext.Push(!canAttackThisTurn);
        try
        {
            await card.OnSummoned(player, _, petCreature);
        }
        finally
        {
            YgoDuelMonsterSummonStyleContext.Pop();
        }

        await card.OnAfterSummonPipelineAsync(player, _, petCreature, canAttackThisTurn);

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

        await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);

        await global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Frontier_Wiseman.SyncBoardAfterAnyDuelSummonAsync(
            YgoChoiceContexts.Blocking(),
            player);

        await EnragedBattleOxService.SyncPlayerPowerAsync(player);

        LegionFiendJesterSpellcasterConduit.RegisterWaivedSummonAfterNormalSpellcasterSummon(
            player,
            card,
            canAttackThisTurn,
            legionCountBeforeSummon);

        if (card.GetType() == typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect.Fushioh_Richie))
            YgoFushiohRichieSummonGate.Consume(player);

        return true;
    }

    private static async Task MoveCardToMonsterPile(Player player, BaseMonsterCard card)
    {
        if (player.PlayerCombatState == null)
            return;

        CardPile targetPile = YgoPlayerPiles.MonsterZone(player);
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
