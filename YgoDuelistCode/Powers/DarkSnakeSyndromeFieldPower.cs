using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Continuous <see cref="Dark_Snake_Syndrome"/>: on the enemy; end of your turn it takes damage; counter doubles (max 64). Removed if the spell leaves the zone.</summary>
public sealed class DarkSnakeSyndromeFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-DARK_SNAKE_SYNDROME_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DARK_SNAKE_SYNDROME_FIELD_POWER.description");

    /// <summary>Removes this power from every enemy that has it from <paramref name="applier"/> (player creature).</summary>
    public static async Task RemoveAllForApplier(Creature applier)
    {
        CombatState? cs = applier.CombatState;
        if (cs == null)
            return;
        foreach (Creature e in cs.HittableEnemies.ToList())
        {
            DarkSnakeSyndromeFieldPower? p = e.GetPower<DarkSnakeSyndromeFieldPower>();
            if (p != null && p.Applier == applier)
                await PowerCmd.Remove(p);
        }
    }

    /// <summary>When the spell is not in the zone (e.g. destroyed), strip matching powers immediately.</summary>
    public static void SyncCleanupIfSpellAbsent(Player player)
    {
        if (player?.Creature == null)
            return;
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone != null && zone.Cards.OfType<Dark_Snake_Syndrome>().Any())
            return;
        TaskHelper.RunSafely(RemoveAllForApplier(player.Creature));
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player)
            return;

        Creature? applier = Applier;
        Player? pl = applier?.Player;
        if (pl == null)
            return;

        if (Owner.Side != CombatSide.Enemy || !Owner.IsAlive)
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        if (zone == null || !zone.Cards.OfType<Dark_Snake_Syndrome>().Any())
        {
            await PowerCmd.Remove(this);
            return;
        }

        Dark_Snake_Syndrome? src = zone.Cards.OfType<Dark_Snake_Syndrome>().FirstOrDefault();
        decimal dmg = Amount;
        if (dmg > 0m)
            await CreatureCmd.Damage(choiceContext, Owner, dmg, ValueProp.Unpowered, applier, src);

        decimal next = System.Math.Min(dmg * 2m, 64m);
        decimal delta = next - Amount;
        if (delta != 0m)
            await PowerCmd.ModifyAmount(this, delta, null, null);
    }
}
