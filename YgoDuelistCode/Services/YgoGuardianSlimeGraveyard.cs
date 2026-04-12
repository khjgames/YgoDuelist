using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Guardian_Slime"/>: when sent to the GY from the hand or field, optionally add 1 <see cref="Ancient_Chant"/> from the draw or discard pile.
/// </summary>
public static class YgoGuardianSlimeGraveyard
{
    private static readonly LocString AddAncientChantPrompt =
        new("cards", "YGODUELIST-GUARDIAN_SLIME.add_ancient_chant");

    public static void OnGuardianSlimeSentToGraveyardFromHandOrField(CardModel guardianCard, PileType from)
    {
        if (guardianCard is not Guardian_Slime)
            return;
        if (from != PileType.Hand && from != MonsterPile.CustomType)
            return;

        Player? player = guardianCard.Owner;
        if (player?.Creature?.CombatState == null)
            return;

        TaskHelper.RunSafely(RunAsync(player));
    }

    private static async Task RunAsync(Player player)
    {
        List<Ancient_Chant> candidates = CollectAncientChants(player);
        if (candidates.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(AddAncientChantPrompt, 0, 1) { Cancelable = true };
        IEnumerable<CardModel> picked;
        try
        {
            picked = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates.Cast<CardModel>().ToList(),
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Ancient_Chant? chosen = picked.OfType<Ancient_Chant>().FirstOrDefault();
        if (chosen == null)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return;

        if (!candidates.Contains(chosen))
            return;

        await CardPileCmd.Add(new[] { chosen }, hand, CardPilePosition.Top, chosen, false);
    }

    private static List<Ancient_Chant> CollectAncientChants(Player player)
    {
        var list = new List<Ancient_Chant>();
        Append(PileType.Draw.GetPile(player), list);
        Append(PileType.Discard.GetPile(player), list);
        return list;
    }

    private static void Append(CardPile? pile, List<Ancient_Chant> list)
    {
        if (pile == null)
            return;
        foreach (CardModel c in pile.Cards)
        {
            if (c is Ancient_Chant ac)
                list.Add(ac);
        }
    }
}
