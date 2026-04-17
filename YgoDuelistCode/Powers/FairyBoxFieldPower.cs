using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

internal static class FairyBoxFieldPowerShared
{
    internal static BaseTrapCard? FaceUpTrapForTier(Player player, bool expectPlus) =>
        SpellTrapZonePile.CustomType.GetPile(player)?.Cards
            .OfType<BaseTrapCard>()
            .FirstOrDefault(c => c.MatchesFairyBoxFieldPowerTier(expectPlus));

    internal static async Task AfterPlayerTurnStartLateAsync(
        YgoDuelistPower self,
        PlayerChoiceContext choiceContext,
        Player player,
        bool expectPlus)
    {
        Creature ownerCreature = self.Owner;
        if (player != ownerCreature.Player || ownerCreature.Side != CombatSide.Player)
            return;

        if (!YgoAnnualTracker.TryConsumeAnnual(player, "FAIRY_BOX_TURN_COIN"))
            return;

        BaseTrapCard? src = FaceUpTrapForTier(player, expectPlus);
        if (src == null)
        {
            await PowerCmd.Remove(self);
            return;
        }

        CombatState? cs = player.Creature?.CombatState;
        if (cs == null)
            return;

        await YgoFairyBoxHeadsTailsWeak.RunStartOfYourTurnAsync(choiceContext, cs, player, ownerCreature, src);
    }

    internal static async Task AfterTurnEndAsync(
        YgoDuelistPower self,
        PlayerChoiceContext choiceContext,
        CombatSide side,
        bool expectPlus)
    {
        Creature ownerCreature = self.Owner;
        if (side != CombatSide.Player || ownerCreature.Side != CombatSide.Player)
            return;

        Player player = ownerCreature.Player;
        if (!YgoAnnualTracker.TryConsumeAnnual(player, "FAIRY_BOX_UPKEEP"))
            return;

        BaseTrapCard? src = FaceUpTrapForTier(player, expectPlus);
        if (src == null)
        {
            await PowerCmd.Remove(self);
            return;
        }

        CombatState? cs = player.Creature?.CombatState;
        if (cs == null)
            return;

        CardModel takeDamage = cs.CreateCard<Fairy_Box_Upkeep_Take_Damage>(player);
        if (src.IsUpgraded)
            takeDamage.UpgradeInternal();

        CardModel destroyTrap = cs.CreateCard<Fairy_Box_Upkeep_Destroy>(player);
        var upkeepOptions = new List<CardModel> { takeDamage, destroyTrap };

        CardModel? pick = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            upkeepOptions,
            player,
            canSkip: false);

        if (pick == null)
            return;

        if (pick is IYgoFairyBoxUpkeepTakeDamageCommand)
        {
            decimal upkeepDamage = src.DynamicVars["Mgc2"].BaseValue;
            await CreatureCmd.Damage(choiceContext, ownerCreature, upkeepDamage, ValueProp.Unpowered, ownerCreature, src);
            return;
        }

        if (pick is IYgoFairyBoxUpkeepDestroyTrapCommand)
            await DestroyTrapAndRemovePowerAsync(player, self, expectPlus);
    }

    private static async Task DestroyTrapAndRemovePowerAsync(Player pl, YgoDuelistPower self, bool expectPlus)
    {
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        BaseTrapCard? box = zone?.Cards.OfType<BaseTrapCard>().FirstOrDefault(c => c.MatchesFairyBoxFieldPowerTier(expectPlus));
        CardPile? gy = GraveyardPile.CustomType.GetPile(pl);
        if (box != null && gy != null)
            await CardPileCmd.Add(new[] { box }, gy, CardPilePosition.Top, box, false);

        YgoSpellTrapZoneBridge.SyncFromZonePile(pl);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(pl);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(pl);
        await PowerCmd.Remove(self);
    }
}

/// <summary>While non-upgraded <see cref="Fairy_Box"/> is face-up.</summary>
public sealed class FairyBoxFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-FAIRY_BOX_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FAIRY_BOX_FIELD_POWER.description");

    /// <summary>Late phase: <see cref="YgoDuelist.YgoDuelistCode.Relics.GraveyardRelic.AfterPlayerTurnStart"/> has cleared annual keys (powers run before relics in the main phase).</summary>
    public override Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player) =>
        FairyBoxFieldPowerShared.AfterPlayerTurnStartLateAsync(this, choiceContext, player, expectPlus: false);

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side) =>
        FairyBoxFieldPowerShared.AfterTurnEndAsync(this, choiceContext, side, expectPlus: false);
}

/// <summary>While upgraded <see cref="Fairy_Box"/> is face-up.</summary>
public sealed class FairyBoxFieldPowerPlus : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-FAIRY_BOX_FIELD_POWER_PLUS.title");

    public override LocString Description => new("powers", "YGODUELIST-FAIRY_BOX_FIELD_POWER_PLUS.description");

    public override Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player) =>
        FairyBoxFieldPowerShared.AfterPlayerTurnStartLateAsync(this, choiceContext, player, expectPlus: true);

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side) =>
        FairyBoxFieldPowerShared.AfterTurnEndAsync(this, choiceContext, side, expectPlus: true);
}
