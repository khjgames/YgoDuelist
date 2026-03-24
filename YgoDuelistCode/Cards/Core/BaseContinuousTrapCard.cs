using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Continuous Trap: activates into the Spell/Trap zone and remains face-up (not sent to Graveyard on activation).
/// </summary>
public abstract class BaseContinuousTrapCard : BaseTrapCard
{
    protected BaseContinuousTrapCard(int cost, CardRarity rarity, TargetType target)
        : base(cost, rarity, target, DuelMonsterRace.TrapContinuous)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && (Pile?.Type != SpellTrapZonePile.CustomType || FaceDown);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null || player.Creature == null)
            return;

        WasSetIntoSpellTrapZone = false;
        FaceDown = false;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        await OnTrapPlay(choiceContext, cardPlay);
        await YgoSpellTrapZoneBridge.ActivateContinuousTrapAsync(this);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
    }
}
