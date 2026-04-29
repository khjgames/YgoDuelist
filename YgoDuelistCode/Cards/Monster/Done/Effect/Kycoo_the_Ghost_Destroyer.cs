using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>When this card attacks: you may banish up to 2 cards from your Graveyard.</summary>
public sealed class Kycoo_the_Ghost_Destroyer : EffectMonsterCard
{
    private static readonly LocString BanishPrompt = new("cards", "YGODUELIST-KYCOO_THE_GHOST_DESTROYER.banish_graveyard");

    public Kycoo_the_Ghost_Destroyer()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 18,
            baseDef: 7,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    protected override async Task BeforeAttackCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        List<CardModel> candidates = BuildGraveyardCandidates(player);
        if (candidates.Count == 0)
            return;

        List<CardModel> picked = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(BanishPrompt, 0, 2)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildGraveyardCandidates(player),
            maxResults: 2);

        foreach (CardModel c in picked)
        {
            if (YgoPlayerPiles.GraveyardContains(player, c))
                await YgoBanishedService.BanishCard(player, c);
        }
    }

    private static List<CardModel> BuildGraveyardCandidates(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player)).ToList();
}
