using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.CardSelection;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Shared graveyard flows for equip spells sent from the Spell/Trap zone.</summary>
public static class YgoEquipGraveyardFromZoneEffects
{
    private const int OptionPayHpReturnTop = 0;

    public static async Task ReturnSelfToDeckTopAsync(Player owner, CardModel source)
    {
        CardPile? draw = YgoPlayerPiles.Draw(owner);
        CardPile? gy = YgoPlayerPiles.Graveyard(owner);
        if (draw == null || gy == null || !gy.Cards.Contains(source))
            return;

        await CardPileCmd.Add(source, draw, CardPilePosition.Top, source, false);
    }

    public static async Task ReturnSelfToHandAsync(Player owner, CardModel source)
    {
        CardPile? hand = YgoPlayerPiles.Hand(owner);
        CardPile? gy = YgoPlayerPiles.Graveyard(owner);
        if (hand == null || gy == null || !gy.Cards.Contains(source))
            return;

        await CardPileCmd.Add(source, hand, CardPilePosition.Bottom, source, false);
    }

    public static async Task<bool> TryOptionalPayHpReturnToDeckTopAsync(
        Player owner,
        CardModel source,
        LocString activatePrompt,
        string payOptionTitleKey,
        string payOptionDescriptionKey,
        int hpCost)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(owner);
        if (gy == null || !gy.Cards.Contains(source))
            return false;

        if (source is not BaseEquipSpellCard equip)
            return false;

        PlayerChoiceContext ctx = YgoChoiceContexts.Blocking();
        var activatePrefs = new CardSelectorPrefs(activatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        CardModel? activationPick = await YgoOrderedCardSelection.TryConfirmSingleCardAsync(
            ctx,
            owner,
            activatePrefs,
            equip);
        if (!ReferenceEquals(activationPick, equip) || !gy.Cards.Contains(source))
            return false;

        if (owner.Creature?.CombatState is not CombatState cs)
            return false;

        YgoTransientSpellOptionCommandCard payOpt = YgoTransientSpellOptionCommandCard.Create(
            cs,
            owner,
            OptionPayHpReturnTop,
            payOptionTitleKey,
            payOptionDescriptionKey,
            source,
            source.PortraitPath);

        List<CardModel> BuildOptions() => new List<CardModel> { payOpt };

        CardModel? picked = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            owner,
            YgoCancelableConfirmGridPrefs.ForSinglePick(activatePrompt),
            BuildOptions);

        if (picked is not YgoTransientSpellOptionCommandCard chosen || chosen.OptionId != OptionPayHpReturnTop)
            return false;

        if (!gy.Cards.Contains(source))
            return false;

        Creature? duelist = owner.Creature;
        if (duelist == null || hpCost <= 0)
            return false;

        int nextHp = duelist.CurrentHp - hpCost;
        if (nextHp < 1)
            nextHp = 1;
        await CreatureCmd.SetCurrentHp(duelist, nextHp);

        await ReturnSelfToDeckTopAsync(owner, source);
        return true;
    }
}
