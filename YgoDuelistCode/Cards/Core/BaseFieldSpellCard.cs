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
/// Field Spell: activates to the dedicated field slot, remains face-up, and applies
/// <see cref="GetFieldStatEffect"/> while active.
/// </summary>
public abstract class BaseFieldSpellCard : BaseSpellCard
{
    protected BaseFieldSpellCard(int cost, CardRarity rarity, TargetType target)
        : base(cost, rarity, target, DuelMonsterRace.SpellField)
    {
    }

    /// <summary>ATK/DEF/level modifiers this field applies to a duel monster (yours).</summary>
    public abstract StatEffectTotal GetFieldStatEffect(BaseMonsterCard target);

    /// <summary>Spell/Trap zone placement as an active field (face-up, normal card frame).</summary>
    public void MarkAsFaceUpFieldInZone() => PrepareSpellForActiveFieldZone();

    protected override bool IsPlayable =>
        base.IsPlayable && Pile?.Type != SpellTrapZonePile.CustomType;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null || player.Creature == null)
            return;

        if (Pile?.Type == SpellTrapZonePile.CustomType)
            return;

        PrepareSpellForActiveFieldZone();

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        await OnSpellPlay(choiceContext, cardPlay);
        await YgoSpellTrapZoneBridge.ActivateFieldSpellFromHandAsync(this);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
    }
}
