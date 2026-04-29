using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// When destroyed as a field monster, after this card hits the Graveyard: choose up to 2 cards there to add to your discard pile.
/// </summary>
public sealed class Keldo : EffectMonsterCard, IYgoCustomFieldMonsterDeathGraveyardRelocation
{
    public override int AttackPortionCount => 2;
    private static readonly LocString GraveyardToDiscardPrompt =
        new("cards", "YGODUELIST-KELDO.destroy.graveyard_select");

    public Keldo()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Normal;

    /// <summary>
    /// Called from <see cref="DuelMonsterPetDeathPatch"/> after the duel monster dies: move equips + this card to GY, then resolve the optional GY → discard selection.
    /// </summary>
    public async Task RunCustomFieldMonsterDeathGraveyardRelocationAsync(Player player, CardPile graveyard)
    {
        if (player == null || graveyard == null)
            return;

        await DuelMonsterPetDeathPatch.MoveEquipsToGraveyardThenMonsterToPileAsync(
            player,
            this,
            graveyard,
            graveyard);

        CardPile? discard = YgoPlayerPiles.Discard(player);
        if (discard == null)
            return;

        List<CardModel> inGrave = BuildGraveyardCandidates(graveyard);
        if (inGrave.Count == 0)
            return;

        int maxSelectable = System.Math.Min(2, inGrave.Count);
        var prefs = new CardSelectorPrefs(GraveyardToDiscardPrompt, 0, maxSelectable)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
        List<CardModel> toDiscard = await YgoOrderedCardSelection.TryChooseManyAsync(
            ctx,
            player,
            prefs,
            () => BuildGraveyardCandidates(graveyard),
            maxResults: maxSelectable);
        if (toDiscard.Count == 0)
            return;

        foreach (CardModel c in toDiscard)
        {
            if (c.Pile?.Type != GraveyardPile.CustomType)
                continue;
            await CardPileCmd.Add(
                new CardModel[] { c },
                discard,
                CardPilePosition.Top,
                c,
                false);
        }
    }

    private static List<CardModel> BuildGraveyardCandidates(CardPile graveyard) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(graveyard.Cards);
}
