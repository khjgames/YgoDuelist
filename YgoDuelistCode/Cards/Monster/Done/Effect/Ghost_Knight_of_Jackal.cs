using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>When this card executes an enemy: optional Special Summon 1 monster from your Graveyard.</summary>
public sealed class Ghost_Knight_of_Jackal : EffectMonsterCard
{
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-GHOST_KNIGHT_OF_JACKAL.summon_from_graveyard");

    public Ghost_Knight_of_Jackal()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 17,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    public override async Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs)
    {
        if (!YgoExecuteKillShared.AnyEnemyExecutedKill(command))
            return;

        Player? player = command.Attacker.Player;
        if (player?.Creature == null)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        if (BuildGraveyardSummonCandidates(player).Count == 0)
            return;

        BlockingPlayerChoiceContext ctx = YgoChoiceContexts.Blocking();
        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(SummonPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildGraveyardSummonCandidates(player));
        if (chosen == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }

    private static List<BaseMonsterCard> BuildGraveyardSummonCandidates(Player player)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return new List<BaseMonsterCard>();

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(gy.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m => m.CanSummonDuelMonster)
            .ToList();
    }
}
