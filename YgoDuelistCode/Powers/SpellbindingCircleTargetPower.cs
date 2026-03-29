using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Marks the enemy bound by face-up <see cref="Spellbinding_Circle"/>: each of your turn starts, apply Spellbinding temp STR and +1 Spellbound.</summary>
public sealed class SpellbindingCircleTargetPower : YgoDuelistPower
{
    public static async Task RemoveAllForApplier(Creature applier)
    {
        var cs = applier.CombatState;
        if (cs == null)
            return;
        foreach (Creature e in cs.HittableEnemies.ToList())
        {
            SpellbindingCircleTargetPower? p = e.GetPower<SpellbindingCircleTargetPower>();
            if (p != null && p.Applier == applier)
                await PowerCmd.Remove(p);
        }
    }

    public static void SyncCleanupIfTrapAbsent(Player player)
    {
        if (player?.Creature == null)
            return;
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone != null && zone.Cards.OfType<Spellbinding_Circle>().Any())
            return;
        TaskHelper.RunSafely(RemoveAllForApplier(player.Creature));
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SPELLBINDING_CIRCLE_TARGET_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SPELLBINDING_CIRCLE_TARGET_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Applier?.Player)
            return;
        if (!Owner.IsAlive)
            return;

        Creature? applier = Applier;
        if (applier == null)
            return;
        Player? pl = applier.Player;
        if (pl == null)
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        Spellbinding_Circle? src = zone?.Cards.OfType<Spellbinding_Circle>().FirstOrDefault();
        if (src == null)
        {
            await PowerCmd.Remove(this);
            return;
        }

        decimal mgc = src.DynamicVars != null && src.DynamicVars.ContainsKey("Mgc")
            ? src.DynamicVars["Mgc"].BaseValue
            : 1m;
        if (mgc > 0m)
        {
            if (src.IsUpgraded)
                await PowerCmd.Apply<SpellbindingTemporaryStrengthPowerPlus>(Owner, mgc, applier, src);
            else
                await PowerCmd.Apply<SpellbindingTemporaryStrengthPower>(Owner, mgc, applier, src);
        }

        await AddSpellboundStacks(Owner, applier, src, 1m);
    }

    private static async Task AddSpellboundStacks(Creature enemy, Creature applier, CardModel src, decimal delta)
    {
        SpellboundPower? a = enemy.GetPower<SpellboundPower>();
        if (a != null)
        {
            await PowerCmd.ModifyAmount(a, delta, applier, src);
            return;
        }

        SpellboundPlusPower? b = enemy.GetPower<SpellboundPlusPower>();
        if (b != null)
            await PowerCmd.ModifyAmount(b, delta, applier, src);
    }
}
