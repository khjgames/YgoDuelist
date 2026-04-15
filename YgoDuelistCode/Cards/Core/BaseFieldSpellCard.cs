using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
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
    /// <summary>Energy orb + unplayable overlay while face-up in the Spell/Trap zone (matches command menu invisible orb).</summary>
    public const string ActiveFaceUpZoneEnergyOrbPath = "YgoDuelist/images/card_frames/Invisible_Energy.png";

    protected BaseFieldSpellCard(int cost, CardRarity rarity, TargetType target)
        : base(cost, rarity, target, DuelMonsterRace.SpellField)
    {
    }

    public override Type[] RelatedCards => GetRelatedCards();

    /// <summary>ATK/DEF/level modifiers this field applies to a duel monster (yours).</summary>
    public abstract StatEffectTotal GetFieldStatEffect(BaseMonsterCard target);

    /// <summary>Spell/Trap zone placement as an active field (face-up, normal card frame).</summary>
    public void MarkAsFaceUpFieldInZone() => PrepareSpellForActiveFieldZone();

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
        await YgoSpellTrapZoneBridge.ActivateFieldSpellFromHandAsync(this);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(player);
    }

    public override string? GetSpellTrapZoneFaceUpEnergyOrbOverridePath(CardPile? pile) =>
        pile?.Type == SpellTrapZonePile.CustomType && !FaceDown ? ActiveFaceUpZoneEnergyOrbPath : null;
}
