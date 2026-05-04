using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Field → Graveyard: once per turn, optionally search 1 monster with printed DEF 15 or less from deck.</summary>
public sealed class Witch_of_the_Black_Forest : EffectMonsterCard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-WITCH_OF_THE_BLACK_FOREST.activate_effect");
    private static readonly LocString SearchPrompt = new("cards", "YGODUELIST-WITCH_OF_THE_BLACK_FOREST.search_deck");
    private static readonly Dictionary<Player, int> LastUsedTurnByPlayer = new();

    public Witch_of_the_Black_Forest()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 11,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Witch_of_the_Black_Forest) };

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        if (from != MonsterPile.CustomType)
            return;
        Player? player = Owner;
        if (player?.Creature == null)
            return;
        TaskHelper.RunSafely(RunFieldToGraveyardSearchAsync(player));
    }

    private async Task RunFieldToGraveyardSearchAsync(Player player)
    {
        int turnStamp = YgoPlayerCombatTurnStamp.Get(player);
        if (LastUsedTurnByPlayer.TryGetValue(player, out int usedStamp) && usedStamp == turnStamp)
            return;

        List<BaseMonsterCard> candidates = BuildSearchCandidates(player);
        if (candidates.Count == 0)
            return;

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            this,
            ActivatePrompt);
        if (ctx == null)
            return;

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(SearchPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildSearchCandidates(player));
        if (chosen == null)
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        await CardPileCmd.Add(new CardModel[] { chosen }, hand, CardPilePosition.Top, chosen, false);
        YgoSanganNameLock.Set(player, chosen.Id.Entry);
        LastUsedTurnByPlayer[player] = turnStamp;
    }

    private static List<BaseMonsterCard> BuildSearchCandidates(Player player) => YgoPlayerPiles
        .OrderedCardsOfTypeFromPiles<BaseMonsterCard>(player, YgoPlayerPiles.Draw)
        .Where(m => m.BaseDef <= 15)
        .ToList();
}
