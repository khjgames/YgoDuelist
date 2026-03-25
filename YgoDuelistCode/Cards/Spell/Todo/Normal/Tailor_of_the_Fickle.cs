using System;
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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Tailor_of_the_Fickle : BaseSpellCard
{
    public Tailor_of_the_Fickle()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null)
            return;

        var zonePile = SpellTrapZonePile.CustomType.GetPile(player);
        if (zonePile == null)
            return;

        List<BaseEquipSpellCard> equippedEquips = zonePile.Cards
            .OfType<BaseEquipSpellCard>()
            .Where(eq => YgoEquipSpellRegistry.GetEquippedMonster(eq) != null)
            .ToList();

        if (equippedEquips.Count == 0)
            return;

        var equipPrefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> equipPick;
        try
        {
            equipPick = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), equippedEquips, player, equipPrefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var equip = equipPick.FirstOrDefault() as BaseEquipSpellCard;
        if (equip == null)
            return;

        var current = YgoEquipSpellRegistry.GetEquippedMonster(equip);
        var monsters = DuelMonsterFieldRegistry.GetFieldMonsters(player).OfType<BaseMonsterCard>().ToList();

        List<BaseMonsterCard> validTargets = monsters
            .Where(m => m != null && !ReferenceEquals(m, current) && equip.CanEquipTo(m))
            .ToList();

        if (validTargets.Count == 0)
            return;

        var targetPrefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> targetPick;
        try
        {
            targetPick = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), validTargets, player, targetPrefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var targetMonster = targetPick.FirstOrDefault() as BaseMonsterCard;
        if (targetMonster == null)
            return;

        YgoEquipSpellRegistry.Attach(equip, targetMonster);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(player);

        if (IsUpgraded)
            await CardPileCmd.Draw(choiceContext, 1, player);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}
