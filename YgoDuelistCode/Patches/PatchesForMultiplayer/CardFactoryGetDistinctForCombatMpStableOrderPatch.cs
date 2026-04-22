using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// MP: <see cref="CardFactory.GetDistinctForCombat"/> uses <see cref="IEnumerableExtensions.TakeRandom"/>, which does
/// <c>ToList().UnstableShuffle(rng).Take(count)</c>. Enumeration order from <see cref="CardPoolModel.GetUnlockedCards"/>
/// is not guaranteed identical on host vs client, so the shuffle input list differs → the same synchronized RNG still
/// yields different 3-card grids (e.g. <see cref="MegaCrit.Sts2.Core.Models.Potions.SkillPotion"/>). Choice sync then
/// maps the same UI index to different <see cref="CardModel.Id"/> and combat checksums diverge.
/// <para/>
/// In multiplayer combat only, mirror vanilla filters then sort by <see cref="CardId.Entry"/> before shuffling so
/// every peer builds the same list; RNG consumption (Fisher–Yates) matches vanilla.
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDistinctForCombat))]
public static class CardFactoryGetDistinctForCombatMpStableOrderPatch
{
    [HarmonyPrefix]
    public static bool Prefix(
        Player player,
        IEnumerable<CardModel> cards,
        int count,
        Rng rng,
        ref IEnumerable<CardModel> __result)
    {
        if (!YgoMpDiagnostics.IsMultiplayer || player?.Creature?.CombatState is not CombatState cs)
            return true;

        IEnumerable<CardModel> afterPlayerCount = FilterForPlayerCount(player.RunState, cards);
        List<CardModel> list = CardFactory.FilterForCombat(afterPlayerCount).ToList();
        list.Sort((a, b) => string.CompareOrdinal(a.Id.Entry, b.Id.Entry));
        list.UnstableShuffle(rng);
        IEnumerable<CardModel> picked = list.Take(count);
        __result = picked.Select(c => cs.CreateCard(c, player));

        YgoMpDiagnostics.VerbosePrint(
            "GetDistinctMP",
            $"player={player.NetId} count={count} rng={rng?.GetType().Name} picks={string.Join(",", list.Take(count).Select(c => c.Id.Entry))}");

        return false;
    }

    private static IEnumerable<CardModel> FilterForPlayerCount(IRunState runState, IEnumerable<CardModel> options)
    {
        if (runState.Players.Count > 1)
            return options.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly);
        return options.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);
    }
}
