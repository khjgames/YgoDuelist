using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>While <see cref="Blind_Destruction"/> is face-up.</summary>
public sealed class BlindDestructionFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-BLIND_DESTRUCTION_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-BLIND_DESTRUCTION_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        if (!YgoAnnualTracker.TryConsumeAnnual(player, "BLIND_DESTRUCTION"))
            return;

        var cs = Owner.CombatState;
        if (cs == null)
            return;

        Blind_Destruction? src = SpellTrapZonePile.CustomType.GetPile(player)?.Cards.OfType<Blind_Destruction>().FirstOrDefault();
        ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(player, src);
        int roll = YgoDeterministicRng.RollDie(cs, 6, "BLIND_DESTRUCTION-D6", mix);
        decimal dmg = roll == 6 ? 12m : roll;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive))
            await CreatureCmd.Damage(choiceContext, e, dmg, ValueProp.Unpowered, Owner, src);

        if (roll == 6)
            return;

        if (player.PlayerCombatState == null)
            return;

        foreach (Creature pet in player.PlayerCombatState.Pets.ToList())
        {
            if (pet is { IsAlive: true })
                await CreatureCmd.Damage(choiceContext, pet, dmg, ValueProp.Unpowered, Owner, src);
        }
    }
}
