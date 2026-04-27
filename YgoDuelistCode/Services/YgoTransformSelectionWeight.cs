using System;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Transform preview and random transform rolls use the same per-card weight as YGO packs
/// (<see cref="YgoDuelistCard.AdjustedPackWeightMultiplier"/>); other cards use weight <c>1</c>.
/// </summary>
public static class YgoTransformSelectionWeight
{
    private const float MinimumWeight = 1e-4f;

    public static float ForCard(CardModel? card) =>
        card is YgoDuelistCard y ? Math.Max(y.AdjustedPackWeightMultiplier, MinimumWeight) : 1f;
}
