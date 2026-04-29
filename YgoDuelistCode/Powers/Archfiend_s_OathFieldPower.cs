using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Archfiend's Oath: once per turn, take 5 blockable damage, declare a card type,
/// then resolve your draw-pile top card into hand or graveyard based on the declaration.
/// </summary>
public sealed class Archfiend_s_Oath_FieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-ARCHFIEND_S_OATH_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-ARCHFIEND_S_OATH_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        CardPile? zone = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.SpellTrapZone(player);
        var activeOaths = zone?.Cards.OfType<Archfiend_s_Oath>().Where(c => !c.FaceDown).ToList() ?? [];
        if (activeOaths.Count == 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        // If somehow multiple copies exist, treat stacks as additional resolves.
        int resolves = (int)Amount;
        resolves = resolves <= 0 ? 1 : resolves;

        var declareOptions = new List<CardModel>
        {
            // Use real card models just for UI + YgoCardType mapping.
            ModelDb.Card<Book_of_Taiyou>(), // Spell
            ModelDb.Card<Anti_Spell>(), // Trap
            ModelDb.Card<Blast_Juggler>(), // Monster
        };

        CardModel? sourceCard = YgoMpCombatOrder.FirstCardWhereStable(activeOaths, _ => true);
        for (int i = 0; i < resolves; i++)
        {
            await CreatureCmd.Damage(choiceContext, Owner, 5m, ValueProp.Unpowered, Owner, sourceCard);

            CardModel? declaredPick = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,
                declareOptions,
                player,
                canSkip: false);

            if (declaredPick is not IYgoCard declared)
                continue;

            var drawPile = player.PlayerCombatState?.DrawPile;
            if (drawPile == null || drawPile.IsEmpty)
                continue;

            CardModel top = drawPile.Cards[0];
            if (top is not IYgoCard topYgo)
                continue;

            bool matches;
            if (declared.YgoCardType == YgoCardType.Spell)
            {
                matches = topYgo.YgoCardType == YgoCardType.Spell;
            }
            else if (declared.YgoCardType == YgoCardType.Trap)
            {
                matches = topYgo.YgoCardType == YgoCardType.Trap;
            }
            else
            {
                matches = topYgo.YgoCardType is YgoCardType.Monster
                    or YgoCardType.EffectMonster
                    or YgoCardType.FusionMonster
                    or YgoCardType.RitualMonster;
            }

            CardPile handPile = player.PlayerCombatState.Hand;
            CardPile? gyPile = YgoDuelist.YgoDuelistCode.Services.YgoPlayerPiles.Graveyard(player);
            if (gyPile == null)
                continue;

            if (matches)
                await CardPileCmd.Add(new[] { top }, handPile, CardPilePosition.Top, sourceCard, false);
            else
                await CardPileCmd.Add(new[] { top }, gyPile, CardPilePosition.Top, sourceCard, false);
        }
    }
}
