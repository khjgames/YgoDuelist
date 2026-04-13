using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Relics;

public sealed class BanishedRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/banished.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/banished.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetBanishedCountForOwner();

    private CardPile? _subscribedPile;

    public override Task BeforeCombatStart()
    {
        SubscribeToBanishedPile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromBanishedPile();
        return Task.CompletedTask;
    }

    public static CardPile? GetBanishedPile(Player? player) => YgoBanishedService.GetPile(player);

    private int GetBanishedCountForOwner()
    {
        Player? player = Owner;
        if (player == null)
            return 0;

        CardPile? pile = GetBanishedPile(player);
        return pile?.Cards.Count ?? 0;
    }

    private void SubscribeToBanishedPile()
    {
        Player? player = Owner;
        if (player == null)
            return;

        CardPile? pile = GetBanishedPile(player);
        if (pile == null)
            return;

        if (_subscribedPile != null)
            UnsubscribeFromBanishedPile();

        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnBanishedContentsChanged;
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromBanishedPile()
    {
        if (_subscribedPile == null)
            return;

        _subscribedPile.ContentsChanged -= OnBanishedContentsChanged;
        _subscribedPile = null;
        InvokeDisplayAmountChanged();
    }

    private void OnBanishedContentsChanged() => InvokeDisplayAmountChanged();

    public static IReadOnlyList<CardModel> GetBanishedCards(Player? player)
    {
        CardPile? pile = GetBanishedPile(player);
        if (pile == null)
            return [];
        return pile.Cards.ToList();
    }

    public static bool IsBanishedRelic(RelicModel? model) => model is BanishedRelic;

    public static BanishedRelic? AsBanished(RelicModel? model) => model as BanishedRelic;
}
