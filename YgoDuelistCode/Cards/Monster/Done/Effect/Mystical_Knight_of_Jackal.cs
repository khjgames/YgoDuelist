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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>When this card executes an enemy: you may place 1 card from your Graveyard on top of your draw pile.</summary>
public sealed class Mystical_Knight_of_Jackal : EffectMonsterCard
{
    private static readonly LocString ReturnPrompt = new("cards", "YGODUELIST-MYSTICAL_KNIGHT_OF_JACKAL.return_to_draw");

    public Mystical_Knight_of_Jackal()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 27,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Warrior;
    public override async Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs)
    {
        if (!YgoExecuteKillShared.AnyEnemyExecutedKill(command))
            return;

        Player? player = command.Attacker.Player;
        if (player?.Creature == null)
            return;

        List<CardModel> candidates = BuildGraveyardCandidates(player);
        if (candidates.Count == 0)
            return;

        BlockingPlayerChoiceContext ctx = YgoChoiceContexts.Blocking();
        CardModel? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(ReturnPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildGraveyardCandidates(player));
        if (chosen == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, draw, CardPilePosition.Top, chosen, false);
    }

    private static List<CardModel> BuildGraveyardCandidates(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player)).ToList();
}
