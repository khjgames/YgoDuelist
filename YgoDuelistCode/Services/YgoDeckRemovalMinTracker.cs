using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Patches;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// After <see cref="MegaCrit.Sts2.Core.Commands.CardPileCmd.RemoveFromDeck"/>, waits one <see cref="SceneTree"/> frame so callers
/// can move the card into trunk/side/extra; then lowers Ygo minimum deck count only if the card is not in those piles or the main deck.
/// Campfire replace removes a card from the run without stashing it — use <see cref="SkipNextRunDeckRemovalMin"/> for that card.
/// </summary>
public static class YgoDeckRemovalMinTracker
{
    private static readonly object SkipLock = new();
    private static readonly HashSet<CardModel> SkipNextDecrement = new();

    public static void SkipNextRunDeckRemovalMin(CardModel card)
    {
        lock (SkipLock)
            SkipNextDecrement.Add(card);
    }

    private static bool TryConsumeSkip(CardModel card)
    {
        lock (SkipLock)
            return SkipNextDecrement.Remove(card);
    }

    /// <summary>
    /// Awaits vanilla removal only, then evaluates on the next frame so callers can still run e.g. trunk <c>AddInternal</c>.
    /// </summary>
    public static async System.Threading.Tasks.Task ChainAfterRemoveFromDeck(
        System.Threading.Tasks.Task inner,
        IReadOnlyList<CardModel> cards)
    {
        await inner;
        if (cards.Count == 0)
            return;

        Player? sample = cards[0].Owner;
        if (sample == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(sample))
            return;

        _ = EvaluateAfterNextFrameAsync(cards);
    }

    private static async System.Threading.Tasks.Task EvaluateAfterNextFrameAsync(IReadOnlyList<CardModel> cards)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        foreach (CardModel c in cards)
        {
            Player? owner = c.Owner;
            if (owner == null || !PlayerRunExtraDeck.IsYgoDuelistPlayer(owner))
                continue;
            bool skip = TryConsumeSkip(c);
            bool inRunPile = IsStillInMainDeckOrYgoStorage(owner, c);
            if (!skip && !inRunPile)
            {
                YgoPlayerMinimumDeck.DecreaseAfterVoluntaryRemovals(owner, 1);
                YgoTopBarDeckCountTextPatch.RefreshDeckCountLabelForPlayer(owner);
            }
        }
    }

    private static bool IsStillInMainDeckOrYgoStorage(Player player, CardModel c)
    {
        if (player.Deck.Cards.Contains(c))
            return true;
        if (PlayerRunTrunk.GetOrCreatePile(player).Cards.Contains(c))
            return true;
        if (PlayerRunSideDeck.GetOrCreatePile(player).Cards.Contains(c))
            return true;
        if (PlayerRunExtraDeck.GetOrCreatePile(player).Cards.Contains(c))
            return true;
        return false;
    }
}
