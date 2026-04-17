using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Dispatches owner turn-start spell/trap zone effects implemented on zone cards.</summary>
public static class YgoJamBreedingMachineContinuous
{
    public static Task TryResolvePlayerTurnStart(PlayerChoiceContext choiceContext, Player player) =>
        TryResolvePlayerTurnStartForPhase(
            choiceContext,
            player,
            YgoOwnerTurnStartSpellTrapDispatchPhase.AfterSealmasterBeforeFieldMonsterHooks);

    public static async Task TryResolvePlayerTurnStartForPhase(
        PlayerChoiceContext choiceContext,
        Player player,
        YgoOwnerTurnStartSpellTrapDispatchPhase phase)
    {
        if (player.PlayerCombatState == null)
            return;

        var zone = SpellTrapZoneRelic.GetSpellTrapZonePile(player);
        if (zone == null)
            return;

        foreach (CardModel card in zone.Cards)
        {
            if (card is not IYgoOwnerTurnStartSpellTrapZoneEffect hook || !hook.IsOwnerTurnStartSpellTrapZoneEffectActive())
                continue;
            if (hook.OwnerTurnStartSpellTrapDispatchPhase != phase)
                continue;
            await hook.TryResolveOwnerTurnStartSpellTrapZoneEffectAsync(choiceContext, player);
        }
    }
}
