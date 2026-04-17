using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Tribute summon with a fixed recipe (e.g. Gate Guardian) instead of generic release count + Mausoleum rows.
/// </summary>
public interface IYgoNamedTripleTributeSummon
{
    bool CanMeetNamedTripleTributeRequirement(Player? player);

    bool NamedTributeRecipeMatches(List<Creature>? pets, int mausoleumHpTributes, int mausoleumHpLossTotal);
}
