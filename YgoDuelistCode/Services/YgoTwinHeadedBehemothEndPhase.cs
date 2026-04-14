using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Twin_Headed_Behemoth"/>: End Phase optional Special Summon from GY if destroyed on the field this turn; ATK/DEF become 10 until it leaves the field.
/// </summary>
public static class YgoTwinHeadedBehemothEndPhase
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-TWIN_HEADED_BEHEMOTH.activate_revive");

    public static void MarkSentFromFieldToGraveyardThisTurn(Player player, Twin_Headed_Behemoth twin)
    {
        if (player == null)
            return;
        twin.MarkEndPhaseReviveEligible(YgoPlayerCombatTurnStamp.Get(player));
    }

    public static async Task TryResolveBeforePlayerTurnEndFlushAsync(PlayerChoiceContext choiceContext, Player player)
    {
        CardPile? gy = GraveyardRelic.GetGraveyardPile(player);
        if (gy == null)
            return;

        int stamp = YgoPlayerCombatTurnStamp.Get(player);
        List<Twin_Headed_Behemoth> twins = gy.Cards.OfType<Twin_Headed_Behemoth>().Where(t => t.IsEndPhaseReviveEligible(stamp)).ToList();
        if (twins.Count == 0)
            return;

        foreach (Twin_Headed_Behemoth twin in twins)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                return;

            var prefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            IEnumerable<CardModel> pick = await CardSelectCmd.FromSimpleGrid(choiceContext, new[] { twin }, player, prefs);
            if (pick.FirstOrDefault() is not Twin_Headed_Behemoth)
                continue;

            if (!gy.Cards.Contains(twin))
                continue;

            twin.ArmReviveSummonMiniStats();
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, twin, choiceContext);
            twin.ClearEndPhaseReviveEligibility();
        }
    }
}
