using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Reads face-up field spells from the Spell/Trap zone pile for stat and level rules.
/// </summary>
public static class YgoFieldSpellStatAggregator
{
    public static IEnumerable<BaseFieldSpellCard> GetActiveFaceUpFieldSpells(Player? player)
    {
        if (player == null)
            yield break;

        // Custom Spell/Trap zone pile is only registered during combat; CardPile.Get throws for
        // SpellTrapZonePile.CustomType (e.g. 11) on deck view / map / compendium.
        if (CombatManager.Instance?.IsInProgress != true)
            yield break;

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            yield break;

        foreach (CardModel c in zone.Cards)
        {
            if (c is BaseFieldSpellCard fs && !fs.FaceDown)
                yield return fs;
        }
    }

    public static void RefreshMonsterSummonKeywords(Player? player)
    {
        if (player == null)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand != null)
        {
            foreach (CardModel c in hand.Cards)
            {
                if (c is BaseMonsterCard m)
                    m.RefreshSummonKeywordsForMonsterLevel();
            }
        }

        foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.GetFieldMonsters(player))
            m.RefreshSummonKeywordsForMonsterLevel();
    }
}
