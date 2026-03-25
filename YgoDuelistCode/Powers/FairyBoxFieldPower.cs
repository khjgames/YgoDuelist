using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>While <see cref="Fairy_Box"/> is face-up.</summary>
public sealed class FairyBoxFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-FAIRY_BOX_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-FAIRY_BOX_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        if (!YgoAnnualTracker.TryConsumeAnnual(player, "FAIRY_BOX_UPKEEP"))
            return;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        Fairy_Box? src = zone?.Cards.OfType<Fairy_Box>().FirstOrDefault(c => !c.FaceDown);
        if (src == null)
        {
            await PowerCmd.Remove(this);
            return;
        }

        CardModel payDamage = ModelDb.Card<Sparks>();
        CardModel destroyTrap = ModelDb.Card<Compulsory_Evacuation_Device>();
        var upkeepOptions = new List<CardModel> { payDamage, destroyTrap };

        CardModel? pick = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            upkeepOptions,
            player,
            canSkip: false);

        if (pick == null)
            return;

        if (pick.Id.Entry == payDamage.Id.Entry)
        {
            await CreatureCmd.Damage(choiceContext, Owner, 5m, ValueProp.Unpowered, Owner, src);
            return;
        }

        if (pick.Id.Entry == destroyTrap.Id.Entry)
            await DestroyTrapAndRemovePowerAsync(player);
    }

    private async Task DestroyTrapAndRemovePowerAsync(Player pl)
    {
        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(pl);
        Fairy_Box? box = zone?.Cards.OfType<Fairy_Box>().FirstOrDefault();
        CardPile? gy = GraveyardPile.CustomType.GetPile(pl);
        if (box != null && gy != null)
            await CardPileCmd.Add(new[] { box }, gy, CardPilePosition.Top, box, false);

        YgoSpellTrapZoneBridge.SyncFromZonePile(pl);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(pl);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(pl);
        await PowerCmd.Remove(this);
    }
}
