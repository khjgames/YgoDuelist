using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="RunState"/> registers deck cards with <c>foreach (card in player.Deck.Cards) AddCard(card, player)</c>.
/// Removing fusion monsters from the deck inside <see cref="RunState.AddCard"/> mutates that list during enumeration
/// and throws <see cref="InvalidOperationException"/>. Relocate fusions after construction (snapshot via <see cref="Enumerable.ToList{TSource}"/>).
/// </summary>
[HarmonyPatch]
public static class RunStateCreateSharedMoveFusionToExtraDeckPatch
{
    static MethodBase TargetMethod() => AccessTools.DeclaredMethod(typeof(RunState), "CreateShared")!;

    static void Postfix(RunState __result, IReadOnlyList<Player> players)
    {
        foreach (Player player in players)
        {
            if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(player))
                continue;

            CardPile runExtra = PlayerRunExtraDeck.GetOrCreatePile(player);
            bool changed = false;
            foreach (CardModel card in player.Deck.Cards.ToList())
            {
                if (card is not FusionMonsterCard)
                    continue;
                if (!player.Deck.Cards.Contains(card))
                    continue;
                player.Deck.RemoveInternal(card, silent: true);
                runExtra.AddInternal(card, -1, silent: true);
                changed = true;
            }

            if (changed)
                ExtraDeckRelic.NotifyRunExtraDeckChanged(player);
        }
    }
}
