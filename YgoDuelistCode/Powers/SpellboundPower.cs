using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Spellbound debuff: at end of the player's turn, enemies take damage equal to stacks (poison timing analogue).
/// </summary>
public sealed class SpellboundPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SPELLBOUND_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SPELLBOUND_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player)
            return;
        if (Owner.Side != CombatSide.Enemy)
            return;
        if (!Owner.IsAlive)
            return;
        if (Amount <= 0)
            return;

        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, dealer: null, cardSource: null);

        bool hasFaceUpSpellbindingCircle = Owner.CombatState.Players.Any(p =>
            SpellTrapZonePile.CustomType.GetPile(p)?.Cards.OfType<Spellbinding_Circle>().Any(c => !c.FaceDown) == true);
        if (!hasFaceUpSpellbindingCircle)
            await PowerCmd.Remove(this);
    }
}

