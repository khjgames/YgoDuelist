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

public sealed class ShadowRealmRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/shadow_realm.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/shadow_realm.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetShadowRealmCountForOwner();

    private CardPile? _subscribedPile;

    public override Task BeforeCombatStart()
    {
        SubscribeToShadowRealmPile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromShadowRealmPile();
        return Task.CompletedTask;
    }

    public static CardPile? GetShadowRealmPile(Player? player) => YgoShadowRealmService.GetPile(player);

    private int GetShadowRealmCountForOwner()
    {
        Player? player = Owner;
        if (player == null)
            return 0;

        CardPile? pile = GetShadowRealmPile(player);
        return pile?.Cards.Count ?? 0;
    }

    private void SubscribeToShadowRealmPile()
    {
        Player? player = Owner;
        if (player == null)
            return;

        CardPile? pile = GetShadowRealmPile(player);
        if (pile == null)
            return;

        if (_subscribedPile != null)
            UnsubscribeFromShadowRealmPile();

        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnShadowRealmContentsChanged;
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromShadowRealmPile()
    {
        if (_subscribedPile == null)
            return;

        _subscribedPile.ContentsChanged -= OnShadowRealmContentsChanged;
        _subscribedPile = null;
        InvokeDisplayAmountChanged();
    }

    private void OnShadowRealmContentsChanged() => InvokeDisplayAmountChanged();

    public static IReadOnlyList<CardModel> GetShadowRealmCards(Player? player)
    {
        CardPile? pile = GetShadowRealmPile(player);
        if (pile == null)
            return [];
        return pile.Cards.ToList();
    }

    public static bool IsShadowRealmRelic(RelicModel? model) => model is ShadowRealmRelic;

    public static ShadowRealmRelic? AsShadowRealm(RelicModel? model) => model as ShadowRealmRelic;
}
