using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// Base-game combat hand selectors use CombatCard wire, but vanilla does not expose that expectation to
/// <see cref="PlayerChoiceSynchronizer"/> before <c>WaitForRemoteChoice</c>. Register it around the vanilla call so
/// stale pre-buffered Index choices from earlier YGO grids cannot satisfy potion/relic/power hand selectors.
/// </summary>
[HarmonyPatch(typeof(CardSelectCmd))]
public static class CardSelectCmdVanillaHandChoiceExpectationPatch
{
    public readonly struct ExpectationState
    {
        public GridCombatMpExpectation.Active? Previous { get; init; }
        public bool Applied { get; init; }
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromHand))]
    [HarmonyPrefix]
    public static void FromHandPrefix(
        Player player,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? filter,
        ref ExpectationState __state)
    {
        __state = TryInstallCombatCardOnlyExpectation(
            player,
            prefs.MinSelect,
            prefs.MaxSelect,
            () => PileType.Hand.GetPile(player).Cards.Where(filter ?? ((CardModel _) => true)).Count(),
            requireManualConfirmation: prefs.RequireManualConfirmation);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromHand))]
    [HarmonyPostfix]
    public static void FromHandPostfix(ExpectationState __state) => Restore(__state);

    [HarmonyPatch(nameof(CardSelectCmd.FromHandForUpgrade))]
    [HarmonyPrefix]
    public static void FromHandForUpgradePrefix(Player player, ref ExpectationState __state)
    {
        __state = TryInstallCombatCardOnlyExpectation(
            player,
            minSelect: 1,
            maxSelect: 1,
            () => PileType.Hand.GetPile(player).Cards.Count(c => c.IsUpgradable),
            requireManualConfirmation: true,
            autoSelectAtOrBelow: 1);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromHandForUpgrade))]
    [HarmonyPostfix]
    public static void FromHandForUpgradePostfix(ExpectationState __state) => Restore(__state);

    private static ExpectationState TryInstallCombatCardOnlyExpectation(
        Player player,
        int minSelect,
        int maxSelect,
        Func<int> candidateCount,
        bool requireManualConfirmation,
        int? autoSelectAtOrBelow = null)
    {
        NetGameType net = RunManager.Instance.NetService.Type;
        if (net != NetGameType.Host && net != NetGameType.Client)
            return default;

        if (IsLocalSelectingPlayer(player) || CardSelectCmd.Selector != null)
            return default;

        int rows = candidateCount();
        if (rows == 0)
            return default;

        if (autoSelectAtOrBelow is int autoThreshold)
        {
            if (rows <= autoThreshold)
                return default;
        }
        else if (!requireManualConfirmation && rows <= minSelect)
            return default;

        GridCombatMpExpectation.Active? previous = GridCombatMpExpectation.Pending.Value;
        GridCombatMpExpectation.Pending.Value = new GridCombatMpExpectation.Active
        {
            OwnerNetId = player.NetId,
            MinSelect = minSelect,
            MaxSelect = maxSelect,
            CandidateRowCount = rows,
            AllowCombatCard = true,
            AllowIndex = false
        };

        GD.Print(
            $"[YgoDuelist][MP][PlayerChoice] Registered vanilla hand CombatCard-only expectation owner={player.NetId} rows={rows} min={minSelect} max={maxSelect}");

        return new ExpectationState
        {
            Previous = previous,
            Applied = true
        };
    }

    private static void Restore(ExpectationState state)
    {
        if (state.Applied)
            GridCombatMpExpectation.Pending.Value = state.Previous;
    }

    private static bool IsLocalSelectingPlayer(Player player) =>
        MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player)
        && RunManager.Instance.NetService.Type != NetGameType.Replay;
}
