using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell;

/// <summary>
/// Foolish Burial – send 1 monster from your draw or discard pile to the Graveyard,
/// then send this card to the Graveyard as well (instead of discard).
/// Mirrors the old Java effect using the new pile system.
/// </summary>
public sealed class Foolish_Burial : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    protected override IEnumerable<DynamicVar> CanonicalVars => Enumerable.Empty<DynamicVar>();
    
    public Foolish_Burial()
        : base(1, CardRarity.Rare, TargetType.Self, DuelMonsterRace.SpellNormal)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Foolish_Burial),
    };

    protected override void OnUpgrade()
    {
        // Match base game (see Eidolon): upgrade energy cost, not Cost field.
        EnergyCost.UpgradeBy(-1);
    }

    // Match base game pattern (see Clash): gate playability via IsPlayable.
    protected override bool IsPlayable =>
        base.IsPlayable &&
        Owner != null &&
        BuildMonsterCandidates(Owner).Count > 0;

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<CardModel> allMonsters = BuildMonsterCandidates(player);
        if (allMonsters.Count == 0)
            return false;

        return await YgoPrePlayGridSelection.TryPrepareSingleCardPayloadAsync<CardModel>(
            player,
            sourceCard,
            allMonsters,
            YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt),
            rebuildCanonicalForRemoteApply: () => BuildMonsterCandidates(player));
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? chosen) || chosen == null)
            return;

        if (!BuildMonsterCandidates(player).Contains(chosen))
            return;

        await SendCardToGraveyard(choiceContext, player, chosen);
    }

    private static List<CardModel> BuildMonsterCandidates(Player player)
    {
        var deck = YgoPlayerPiles.Draw(player);
        var discard = YgoPlayerPiles.Discard(player);
        var result = new List<CardModel>();
        if (deck != null)
            result.AddRange(YgoMpCombatOrder.CardsSnapshotOrderedForMp(deck.Cards).Where(c => c is BaseMonsterCard));
        if (discard != null)
            result.AddRange(YgoMpCombatOrder.CardsSnapshotOrderedForMp(discard.Cards).Where(c => c is BaseMonsterCard));
        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(result);
    }

    private static async Task SendCardToGraveyard(PlayerChoiceContext choiceContext, Player player, CardModel card)
    {
        var graveyardPile = YgoPlayerPiles.Graveyard(player);
        if (graveyardPile == null)
            return;

        await CardPileCmd.Add(
            new[] { card },
            graveyardPile,
            CardPilePosition.Top,
            card,
            false);
    }
}
