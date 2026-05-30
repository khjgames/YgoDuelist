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

/// <summary>When this card executes an enemy: you may place 1 Monster card from your Graveyard on top of your draw pile.</summary>
public sealed class Winged_Sage_Falcos : EffectMonsterCard
{
    private static readonly LocString ReturnPrompt = new("cards", "YGODUELIST-WINGED_SAGE_FALCOS.return_to_draw");

    public Winged_Sage_Falcos()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 17,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }

    public override async Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs)
    {
        if (!YgoExecuteKillShared.AnyEnemyExecutedKill(command))
            return;

        if (command.Attacker == null)
            return;
        Player? player = command.Attacker.Player;
        if (player?.Creature == null)
            return;

        List<BaseMonsterCard> candidates = BuildGraveyardMonsterCandidates(player);
        if (candidates.Count == 0)
            return;

        BlockingPlayerChoiceContext ctx = YgoChoiceContexts.Blocking();
        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(ReturnPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildGraveyardMonsterCandidates(player));
        if (chosen == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, draw, CardPilePosition.Top, chosen, false);
    }

    private static List<BaseMonsterCard> BuildGraveyardMonsterCandidates(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .ToList();
}
