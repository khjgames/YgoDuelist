using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// While <see cref="Stumbling"/> is face-up in the Spell/Trap zone: at the start of each of your turns, enemies lose temporary Strength equal to that spell's <c>Mgc</c> (once per round).
/// </summary>
public sealed class StumblingFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-STUMBLING_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-STUMBLING_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        await ApplyStumblingDebuffAsync();
    }

    private async Task ApplyStumblingDebuffAsync()
    {
        var cs = Owner.CombatState;
        if (cs == null)
            return;

        decimal loss = GetStrengthLossPerTick();
        if (loss <= 0m)
            return;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive))
            await PowerCmd.Apply<YgoTemporaryStrengthLossPower>(e, loss, Owner, null);
    }

    private decimal GetStrengthLossPerTick()
    {
        Creature? applier = Applier;
        Player? pl = applier?.Player;
        if (pl == null)
            return 0m;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        Stumbling? src = zone?.Cards.OfType<Stumbling>().FirstOrDefault();
        if (src?.DynamicVars != null && src.DynamicVars.ContainsKey("Mgc"))
            return src.DynamicVars["Mgc"].BaseValue;
        return 0m;
    }
}
