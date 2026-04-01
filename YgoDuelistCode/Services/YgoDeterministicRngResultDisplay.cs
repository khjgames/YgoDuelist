using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Ephemeral command cards for <see cref="CardSelectCmd.FromSimpleGrid"/> previews of
/// <see cref="YgoDeterministicRng"/> coin / d6 outcomes (Heads, Tails, Rolled_1–6 art).
/// </summary>
public static class YgoDeterministicRngResultDisplay
{
    /// <summary>Single sample d6 outcome card for dice-effect tooltips (not all six faces).</summary>
    public static IHoverTip Rolled6SampleHoverTip() =>
        HoverTipFactory.FromCard(YgoPackCardCatalog.CardFromType(typeof(Rolled_6)));

    public static MonsterCommandCard CreateCoinFlipResultCard(CombatState combatState, Player player, bool flipIsHeads) =>
        flipIsHeads
            ? combatState.CreateCard<Heads>(player)
            : combatState.CreateCard<Tails>(player);

    public static MonsterCommandCard CreateD6RollResultCard(CombatState combatState, Player player, int roll) =>
        roll switch
        {
            1 => combatState.CreateCard<Rolled_1>(player),
            2 => combatState.CreateCard<Rolled_2>(player),
            3 => combatState.CreateCard<Rolled_3>(player),
            4 => combatState.CreateCard<Rolled_4>(player),
            5 => combatState.CreateCard<Rolled_5>(player),
            _ => combatState.CreateCard<Rolled_6>(player),
        };
}
