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
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

internal static class DarkSnakeSyndromeFieldPowerShared
{
    public static async Task RemoveAllForApplier(Creature applier)
    {
        CombatState? cs = applier.CombatState;
        if (cs == null)
            return;
        foreach (Creature e in YgoMpCombatOrder.CreatureListOrderedByCombatId(cs.HittableEnemies))
        {
            DarkSnakeSyndromeFieldPower? a = e.GetPower<DarkSnakeSyndromeFieldPower>();
            if (a != null && a.Applier == applier)
                await PowerCmd.Remove(a);
            DarkSnakeSyndromeFieldPowerPlus? b = e.GetPower<DarkSnakeSyndromeFieldPowerPlus>();
            if (b != null && b.Applier == applier)
                await PowerCmd.Remove(b);
        }
    }

    public static void SyncCleanupIfSpellAbsent(Player player)
    {
        if (player?.Creature == null)
            return;
        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(player);
        if (zone != null && zone.Cards.OfType<Dark_Snake_Syndrome>().Any())
            return;
        TaskHelper.RunSafely(RemoveAllForApplier(player.Creature));
    }

    public static async Task AfterTurnEnd(YgoDuelistPower self, PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player)
            return;

        Creature? applier = self.Applier;
        Player? pl = applier?.Player;
        if (pl == null)
            return;

        if (self.Owner.Side != CombatSide.Enemy || !self.Owner.IsAlive)
            return;

        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(pl);
        if (zone == null || !zone.Cards.OfType<Dark_Snake_Syndrome>().Any())
        {
            await PowerCmd.Remove(self);
            return;
        }

        Dark_Snake_Syndrome? src =
            YgoMpCombatOrder.FirstCardWhereStable(zone.Cards, c => c is Dark_Snake_Syndrome) as Dark_Snake_Syndrome;
        decimal cap = GetStackDamageCap(src);
        decimal dmg = System.Math.Min(self.Amount, cap);
        if (dmg > 0m)
            await CreatureCmd.Damage(choiceContext, self.Owner, dmg, ValueProp.Unpowered, applier, src);

        decimal next = System.Math.Min(dmg * 2m, cap);
        decimal delta = next - self.Amount;
        if (delta != 0m)
            await PowerCmd.ModifyAmount(self, delta, null, null);
    }

    private static decimal GetStackDamageCap(Dark_Snake_Syndrome? src)
    {
        if (src?.DynamicVars != null && src.DynamicVars.ContainsKey("Mgc"))
            return src.DynamicVars["Mgc"].BaseValue;
        return 32m;
    }
}

/// <summary>Continuous <see cref="Dark_Snake_Syndrome"/> (base): counter doubles each end of your turn, capped by the field spell's <c>Mgc</c> (32).</summary>
public sealed class DarkSnakeSyndromeFieldPower : YgoDuelistPower
{
    public static Task RemoveAllForApplier(Creature applier) => DarkSnakeSyndromeFieldPowerShared.RemoveAllForApplier(applier);

    public static void SyncCleanupIfSpellAbsent(Player player) => DarkSnakeSyndromeFieldPowerShared.SyncCleanupIfSpellAbsent(player);

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-DARK_SNAKE_SYNDROME_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-DARK_SNAKE_SYNDROME_FIELD_POWER.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side) =>
        await DarkSnakeSyndromeFieldPowerShared.AfterTurnEnd(this, choiceContext, side);
}

/// <summary>Continuous upgraded <see cref="Dark_Snake_Syndrome"/>: same behavior; UI states 64 cap.</summary>
public sealed class DarkSnakeSyndromeFieldPowerPlus : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-DARK_SNAKE_SYNDROME_FIELD_POWER_PLUS.title");

    public override LocString Description => new("powers", "YGODUELIST-DARK_SNAKE_SYNDROME_FIELD_POWER_PLUS.description");

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side) =>
        await DarkSnakeSyndromeFieldPowerShared.AfterTurnEnd(this, choiceContext, side);
}
