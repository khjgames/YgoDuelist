using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Des_Feral_Imp : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipEffectHoverTitle = new("card_keywords", "20041.title");
    private static readonly LocString FlipActivationPrompt =
        new("cards", "YGODUELIST-DES_FERAL_IMP.flip_preview.activation");

    private static readonly LocString FlipGraveyardPrompt =
        new("cards", "YGODUELIST-DES_FERAL_IMP.flip_preview.graveyard");

    public override bool UseAlternateUpgradedDescription => true;

    public Des_Feral_Imp()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 16,
            baseDef: 18,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Reptile)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Draw;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Des_Feral_Imp),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            LocString flipDesc = new("cards", "YGODUELIST-DES_FERAL_IMP.flip_effect.description");
            flipDesc.Add("Mgc", DynamicVars["Mgc"].BaseValue);
            yield return new HoverTip(FlipEffectHoverTitle, flipDesc);
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Des_Feral_Imp || Owner == null || Owner.Creature?.CombatState == null)
            return;

        Player player = Owner;

        var activationPrefs = new CardSelectorPrefs(FlipActivationPrompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, new[] { self }, player, activationPrefs);

        int maxPick = (int)DynamicVars["Mgc"].BaseValue;
        if (maxPick <= 0)
            return;

        CardPile? gravePile = YgoPlayerPiles.Graveyard(player);
        if (gravePile == null)
            return;
        List<CardModel> BuildGraveyardTargets()
        {
            CardPile? latestGravePile = YgoPlayerPiles.Graveyard(player);
            return latestGravePile == null
                ? []
                : YgoMpCombatOrder.CardsSnapshotOrderedForMp(latestGravePile.Cards);
        }

        List<CardModel> inGrave = BuildGraveyardTargets();
        if (inGrave.Count == 0)
            return;

        int maxSelectable = System.Math.Min(maxPick, inGrave.Count);
        var gravePrefs = new CardSelectorPrefs(FlipGraveyardPrompt, 0, maxSelectable)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        List<CardModel> toShuffle = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            gravePrefs,
            BuildGraveyardTargets,
            maxResults: maxSelectable);
        if (toShuffle.Count == 0)
            return;

        CardPile? drawPile = YgoPlayerPiles.Draw(player);
        if (drawPile == null)
            return;
        foreach (CardModel card in toShuffle)
            await CardPileCmd.Add(card, drawPile, CardPilePosition.Random, card, false);

        await CardPileCmd.Shuffle(choiceContext, player);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
