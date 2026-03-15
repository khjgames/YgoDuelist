using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Relics;

public sealed class GraveyardRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/graveyard.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/graveyard.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetGraveyardCountForOwner();

    private CardPile? _subscribedPile;

    public override Task BeforeCombatStart()
    {
        SubscribeToGraveyardPile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromGraveyardPile();
        return Task.CompletedTask;
    }

    /// <summary>Gets the graveyard pile for the current combat player, or null if not in combat.</summary>
    public static CardPile? GetGraveyardPile(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;
        return CustomPiles.GetCustomPile(player.PlayerCombatState, GraveyardPile.CustomType);
    }

    /// <summary>Graveyard card count for this relic's owner (for relic display).</summary>
    private int GetGraveyardCountForOwner()
    {
        var player = Owner;
        if (player == null)
            return 0;

        var pile = GetGraveyardPile(player);
        return pile?.Cards.Count ?? 0;
    }

    private void SubscribeToGraveyardPile()
    {
        var player = Owner;
        if (player == null)
            return;

        var pile = GetGraveyardPile(player);
        if (pile == null)
            return;

        // Avoid double-subscription.
        if (_subscribedPile != null)
            UnsubscribeFromGraveyardPile();

        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnGraveyardContentsChanged;
        // Force initial refresh.
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromGraveyardPile()
    {
        if (_subscribedPile == null)
            return;

        _subscribedPile.ContentsChanged -= OnGraveyardContentsChanged;
        _subscribedPile = null;
        InvokeDisplayAmountChanged();
    }

    private void OnGraveyardContentsChanged()
    {
        InvokeDisplayAmountChanged();
    }

    /// <summary>Cards in the graveyard pile for the given player (for relic click view).</summary>
    public static IReadOnlyList<CardModel> GetGraveyardCards(Player? player)
    {
        var pile = GetGraveyardPile(player);
        if (pile == null) return [];
        return pile.Cards.ToList();
    }

    public static bool IsGraveyardRelic(RelicModel? model) => model is GraveyardRelic;

    public static GraveyardRelic? AsGraveyard(RelicModel? model) => model as GraveyardRelic;
}
