using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Arsenal_Robber : BaseTrapCard
{
    public Arsenal_Robber()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner?.PlayerCombatState != null
        && (
            Owner.PlayerCombatState.DrawPile.Cards.OfType<BaseEquipSpellCard>().Any()
            || Owner.PlayerCombatState.DiscardPile.Cards.OfType<BaseEquipSpellCard>().Any()
        );

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null)
            return;

        var drawPile = Owner.PlayerCombatState.DrawPile;
        var discardPile = Owner.PlayerCombatState.DiscardPile;
        List<BaseEquipSpellCard> equipSpells = drawPile.Cards
            .OfType<BaseEquipSpellCard>()
            .Concat(discardPile.Cards.OfType<BaseEquipSpellCard>())
            .ToList();

        if (equipSpells.Count == 0)
            return;

        List<CardModel> options = equipSpells.Cast<CardModel>().ToList();

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = false,
            Cancelable = true
        };

        IEnumerable<CardModel> picked = await CardSelectCmd.FromSimpleGrid(choiceContext, options, Owner, prefs);
        CardModel? chosen = picked.FirstOrDefault();
        if (chosen == null)
            return;

        CardPile? grave = GraveyardPile.CustomType.GetPile(Owner);
        if (grave == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, grave, CardPilePosition.Top, chosen, false);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}
