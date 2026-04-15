using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
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
        ColdWaveSpellTrapLockGate.MarkPlayerUsedSpellTrapThisTurn(Owner);
        Player? player = Owner;
        if (player == null || player.Creature == null)
            return;

        WasSetIntoSpellTrapZone = false;
        FaceDown = false;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        await OnTrapPlay(choiceContext, cardPlay);
        await YgoSpellTrapZoneBridge.ActivateContinuousTrapAsync(this);
        await OnAfterContinuousTrapEnteredSpellTrapZoneAsync(choiceContext, cardPlay);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
    }

    public override string? GetSpellTrapZoneFaceUpEnergyOrbOverridePath(CardPile? pile) =>
        pile?.Type == SpellTrapZonePile.CustomType && !FaceDown ? BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath : null;

    /// <summary>After the card is in the Spell/Trap zone (e.g. <see cref="YgoSpellTrapEquipLinkRegistry"/> attach).</summary>
    protected virtual Task OnAfterContinuousTrapEnteredSpellTrapZoneAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;
}
