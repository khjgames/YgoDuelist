using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Relics;

/// <summary>
/// Debug/utility relic that exposes the YgoCardOptionPile.
/// Clicking it opens a read-only grid view of whatever cards are currently
/// in the option pile for this player.
/// </summary>
public sealed class CardOptionsRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/card_options.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/card_options.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetOptionCountForOwner();

    private CardPile? _subscribedPile;

    /// <summary>Gets the YgoCardOption pile for the current combat player, or null if not in combat.</summary>
    public static CardPile? GetOptionPile(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;
        return CustomPiles.GetCustomPile(player.PlayerCombatState, YgoCardOptionPile.CustomType);
    }

    private int GetOptionCountForOwner()
    {
        var player = Owner;
        if (player == null)
            return 0;
        var pile = GetOptionPile(player);
        return pile?.Cards.Count ?? 0;
    }

    public override Task BeforeCombatStart()
    {
        SubscribeToOptionPile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromOptionPile();
        return Task.CompletedTask;
    }

    private void SubscribeToOptionPile()
    {
        var player = Owner;
        if (player == null)
            return;

        var pile = GetOptionPile(player);
        if (pile == null)
            return;

        if (_subscribedPile != null)
            UnsubscribeFromOptionPile();

        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnOptionContentsChanged;
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromOptionPile()
    {
        if (_subscribedPile == null)
            return;

        _subscribedPile.ContentsChanged -= OnOptionContentsChanged;
        _subscribedPile = null;
        InvokeDisplayAmountChanged();
    }

    private void OnOptionContentsChanged()
    {
        InvokeDisplayAmountChanged();
    }

    /// <summary>Cards in the option pile for the given player.</summary>
    public static IReadOnlyList<CardModel> GetOptionCards(Player? player)
    {
        var pile = GetOptionPile(player);
        if (pile == null) return [];
        return pile.Cards;
    }

    public static bool IsCardOptionsRelic(RelicModel? model) => model is CardOptionsRelic;

    public static CardOptionsRelic? AsCardOptions(RelicModel? model) => model as CardOptionsRelic;
}
