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
/// Continuous Spell: activates into a Spell/Trap zone slot, remains face-up, and applies
/// <see cref="GetContinuousStatEffect"/> while active.
/// </summary>
public abstract class BaseContinuousSpellCard : BaseSpellCard
{
    protected BaseContinuousSpellCard(int cost, CardRarity rarity, TargetType target)
        : base(cost, rarity, target, DuelMonsterRace.SpellContinuous)
    {
    }

    /// <summary>ATK/DEF/level modifiers this continuous spell applies to a duel monster you control.</summary>
    public abstract StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target);

    protected override bool IsPlayable =>
        base.IsPlayable
        && (Pile?.Type != SpellTrapZonePile.CustomType || FaceDown);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ColdWaveSpellTrapLockGate.MarkPlayerUsedSpellTrapThisTurn(Owner);
        Player? player = Owner;
        if (player == null || player.Creature == null)
            return;

        PrepareSpellForActiveFieldZone();

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        await OnSpellPlay(choiceContext, cardPlay);
        await YgoCurseOfDarknessSpellHook.AfterSpellResolved(choiceContext, this);
        await YgoSpellTrapZoneBridge.ActivateContinuousSpellAsync(this);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
    }
}
