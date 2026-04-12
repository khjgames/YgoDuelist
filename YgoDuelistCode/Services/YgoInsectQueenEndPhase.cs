using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>End Phase: if <see cref="Insect_Queen"/> scored a battle kill this turn, Special Summon 1 Insect Monster Token.</summary>
public static class YgoInsectQueenEndPhase
{
    public static async Task TryResolveBeforePlayerTurnEndFlushAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState == null)
            return;

        foreach (Insect_Queen q in DuelMonsterFieldRegistry.GetFieldMonsters(player).OfType<Insect_Queen>())
        {
            if (!q.PendingInsectMonsterTokenEndPhase)
                continue;
            q.PendingInsectMonsterTokenEndPhase = false;
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                continue;
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Insect_Monster_Token>(player, choiceContext, defensePosition: false);
        }
    }
}
