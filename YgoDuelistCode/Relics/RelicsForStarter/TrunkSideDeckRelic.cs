using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Relics;

public sealed class TrunkSideDeckRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/trunk_side_deck.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/trunk_side_deck.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetTrunkSideCountForOwner();

    private CardPile? _subscribedTrunk;
    private CardPile? _subscribedSide;

    public static void NotifyRunTrunkSideChanged(Player? player)
    {
        if (player == null)
            return;
        foreach (RelicModel r in player.Relics)
        {
            if (r is TrunkSideDeckRelic ts)
                ts.InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterObtained()
    {
        SubscribeToRunPiles();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved()
    {
        UnsubscribeFromRunPiles();
        return Task.CompletedTask;
    }

    private int GetTrunkSideCountForOwner()
    {
        Player? player = Owner;
        if (player == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return 0;

        return PlayerRunTrunk.GetOrCreatePile(player).Cards.Count
               + PlayerRunSideDeck.GetOrCreatePile(player).Cards.Count;
    }

    private void SubscribeToRunPiles()
    {
        UnsubscribeFromRunPiles();
        Player? player = Owner;
        if (player == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return;

        _subscribedTrunk = PlayerRunTrunk.GetOrCreatePile(player);
        _subscribedSide = PlayerRunSideDeck.GetOrCreatePile(player);
        _subscribedTrunk.ContentsChanged += OnTrunkSideContentsChanged;
        _subscribedSide.ContentsChanged += OnTrunkSideContentsChanged;
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromRunPiles()
    {
        if (_subscribedTrunk != null)
            _subscribedTrunk.ContentsChanged -= OnTrunkSideContentsChanged;
        if (_subscribedSide != null)
            _subscribedSide.ContentsChanged -= OnTrunkSideContentsChanged;
        _subscribedTrunk = null;
        _subscribedSide = null;
        InvokeDisplayAmountChanged();
    }

    private void OnTrunkSideContentsChanged()
    {
        InvokeDisplayAmountChanged();
    }

    public static IReadOnlyList<CardModel> GetTrunkCards(Player? player)
    {
        if (player == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return [];
        return PlayerRunTrunk.GetOrCreatePile(player).Cards.ToList();
    }

    public static IReadOnlyList<CardModel> GetSideDeckCards(Player? player)
    {
        if (player == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
            return [];
        return PlayerRunSideDeck.GetOrCreatePile(player).Cards.ToList();
    }

    public static bool IsTrunkSideDeckRelic(RelicModel? model) => model is TrunkSideDeckRelic;

    public static TrunkSideDeckRelic? AsTrunkSideDeck(RelicModel? model) => model as TrunkSideDeckRelic;
}
