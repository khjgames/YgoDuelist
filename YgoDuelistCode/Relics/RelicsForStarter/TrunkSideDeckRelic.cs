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
        foreach (TrunkSideDeckRelic ts in YgoPlayerRelicAccess.GetRelics<TrunkSideDeckRelic>(player))
        {
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
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return 0;

        return (YgoPlayerRunPiles.Trunk(player)?.Cards.Count ?? 0)
               + (YgoPlayerRunPiles.SideDeck(player)?.Cards.Count ?? 0);
    }

    private void SubscribeToRunPiles()
    {
        UnsubscribeFromRunPiles();
        Player? player = Owner;
        if (!YgoPlayerRunPiles.IsYgoRunPlayer(player))
            return;

        _subscribedTrunk = YgoPlayerRunPiles.Trunk(player);
        _subscribedSide = YgoPlayerRunPiles.SideDeck(player);
        if (_subscribedTrunk == null || _subscribedSide == null)
            return;
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
        return YgoPlayerRunPiles.TrunkCards(player);
    }

    public static IReadOnlyList<CardModel> GetSideDeckCards(Player? player)
    {
        return YgoPlayerRunPiles.SideDeckCards(player);
    }

    public static bool IsTrunkSideDeckRelic(RelicModel? model) => model is TrunkSideDeckRelic;

    public static TrunkSideDeckRelic? AsTrunkSideDeck(RelicModel? model) => model as TrunkSideDeckRelic;
}
