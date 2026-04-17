using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

internal static class RushReliablePreSpendSelection
{
    public static async Task<bool> TryPrepareAsync(CardModel card, Player player)
    {
        var candidates = DuelMonsterFieldRegistry.GetFieldMonsters(player)
            .OfType<BaseMonsterCard>()
            .ToList();
        if (candidates.Count == 0)
            return false;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        var pick = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), candidates, player, prefs);
        var selected = pick.OfType<BaseMonsterCard>().FirstOrDefault();
        if (selected == null)
            return false;

        RushReliablePlayPayload.SetPending(card, selected);
        return true;
    }
}
