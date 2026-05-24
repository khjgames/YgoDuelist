using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Discoverable pool of <see cref="BaseYgoPowerCard"/> templates for combat bonus offers.</summary>
public static class YgoPowerCardCatalog
{
    private static readonly object Gate = new();
    private static List<CardModel>? sPowerTemplates;

    public static IReadOnlyList<CardModel> GetAllPowerCardTemplates()
    {
        lock (Gate)
        {
            if (sPowerTemplates != null)
                return sPowerTemplates;

            var list = new List<CardModel>();
            foreach (System.Type t in typeof(YgoDuelistCard).Assembly.GetTypes())
            {
                if (t.IsAbstract || !t.IsSubclassOf(typeof(BaseYgoPowerCard)))
                    continue;

                try
                {
                    list.Add(YgoPackCardCatalog.CardFromType(t));
                }
                catch
                {
                    // Skip types that are not registered card models.
                }
            }

            sPowerTemplates = list;
            return sPowerTemplates;
        }
    }

    public static List<CardModel> GetUnlockedPowerCardTemplates(Player player)
    {
        HashSet<ModelId> unlocked = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Select(c => c.Id)
            .ToHashSet();

        return GetAllPowerCardTemplates()
            .Where(c => unlocked.Contains(c.Id))
            .Where(c => !YgoPackCardCatalog.IsYgoBlockedFromMultiplayerProceduralPools(player, c))
            .ToList();
    }
}
