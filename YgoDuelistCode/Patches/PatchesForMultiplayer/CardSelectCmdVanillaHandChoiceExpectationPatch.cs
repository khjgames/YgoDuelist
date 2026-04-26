using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
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

    [HarmonyPatch(nameof(CardSelectCmd.FromSimpleGrid))]
    [HarmonyPrefix]
    public static void FromSimpleGridPrefix(IReadOnlyList<CardModel> cardsIn, Player player, CardSelectorPrefs prefs, ref ExpectationState __state)
    {
        int rows = cardsIn.Count;
        __state = TryInstallIndexExpectation(
            player,
            prefs.MinSelect,
            prefs.MaxSelect,
            rows,
            requireManualConfirmation: prefs.RequireManualConfirmation,
            allowNegativeIndex: false,
            allowEmptyIndexResult: prefs.Cancelable);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromSimpleGrid))]
    [HarmonyPostfix]
    public static void FromSimpleGridPostfix(ExpectationState __state) => Restore(__state);

    [HarmonyPatch(nameof(CardSelectCmd.FromSimpleGridForRewards))]
    [HarmonyPrefix]
    public static void FromSimpleGridForRewardsPrefix(List<CardCreationResult> cards, Player player, CardSelectorPrefs prefs, ref ExpectationState __state)
    {
        __state = TryInstallIndexExpectation(
            player,
            prefs.MinSelect,
            prefs.MaxSelect,
            cards.Count,
            requireManualConfirmation: prefs.RequireManualConfirmation,
            allowNegativeIndex: false,
            allowEmptyIndexResult: prefs.Cancelable);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromSimpleGridForRewards))]
    [HarmonyPostfix]
    public static void FromSimpleGridForRewardsPostfix(ExpectationState __state) => Restore(__state);

    [HarmonyPatch(nameof(CardSelectCmd.FromChooseACardScreen))]
    [HarmonyPrefix]
    public static void FromChooseACardScreenPrefix(IReadOnlyList<CardModel> cards, Player player, bool canSkip, ref ExpectationState __state)
    {
        __state = TryInstallIndexExpectation(
            player,
            minSelect: 1,
            maxSelect: 1,
            rows: cards.Count,
            requireManualConfirmation: true,
            allowNegativeIndex: canSkip,
            allowEmptyIndexResult: false);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromChooseACardScreen))]
    [HarmonyPostfix]
    public static void FromChooseACardScreenPostfix(ExpectationState __state) => Restore(__state);

    [HarmonyPatch(nameof(CardSelectCmd.FromChooseABundleScreen))]
    [HarmonyPrefix]
    public static void FromChooseABundleScreenPrefix(Player player, IReadOnlyList<IReadOnlyList<CardModel>> bundles, ref ExpectationState __state)
    {
        __state = TryInstallIndexExpectation(
            player,
            minSelect: 1,
            maxSelect: 1,
            rows: bundles.Count,
            requireManualConfirmation: true,
            allowNegativeIndex: false,
            allowEmptyIndexResult: false);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromChooseABundleScreen))]
    [HarmonyPostfix]
    public static void FromChooseABundleScreenPostfix(ExpectationState __state) => Restore(__state);

    [HarmonyPatch(nameof(CardSelectCmd.FromDeckForUpgrade))]
    [HarmonyPrefix]
    public static void FromDeckForUpgradePrefix(Player player, CardSelectorPrefs prefs, ref ExpectationState __state)
    {
        int rows = PileType.Deck.GetPile(player).Cards.Count(c => c.IsUpgradable);
        __state = TryInstallDeckCardExpectation(player, prefs.MinSelect, prefs.MaxSelect, rows, prefs.RequireManualConfirmation);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromDeckForUpgrade))]
    [HarmonyPostfix]
    public static void FromDeckForUpgradePostfix(ExpectationState __state) => Restore(__state);

    [HarmonyPatch(nameof(CardSelectCmd.FromDeckForTransformation))]
    [HarmonyPrefix]
    public static void FromDeckForTransformationPrefix(Player player, CardSelectorPrefs prefs, Func<CardModel, CardTransformation>? cardToTransformation, ref ExpectationState __state)
    {
        int rows = PileType.Deck.GetPile(player).Cards.Count(c => c.Type != CardType.Quest && c.IsTransformable);
        __state = TryInstallDeckCardExpectation(player, prefs.MinSelect, prefs.MaxSelect, rows, prefs.RequireManualConfirmation);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromDeckForTransformation))]
    [HarmonyPostfix]
    public static void FromDeckForTransformationPostfix(ExpectationState __state) => Restore(__state);

    [HarmonyPatch(nameof(CardSelectCmd.FromDeckGeneric))]
    [HarmonyPrefix]
    public static void FromDeckGenericPrefix(Player player, CardSelectorPrefs prefs, Func<CardModel, bool>? filter, ref ExpectationState __state)
    {
        int rows = PileType.Deck.GetPile(player).Cards.Count(c => filter == null || filter(c));
        __state = TryInstallDeckCardExpectation(player, prefs.MinSelect, prefs.MaxSelect, rows, prefs.RequireManualConfirmation);
    }

    [HarmonyPatch(nameof(CardSelectCmd.FromDeckGeneric))]
    [HarmonyPostfix]
    public static void FromDeckGenericPostfix(ExpectationState __state) => Restore(__state);

    private static System.Reflection.MethodBase FromDeckForEnchantmentTarget() =>
        AccessTools.Method(
            typeof(CardSelectCmd),
            nameof(CardSelectCmd.FromDeckForEnchantment),
            new[]
            {
                typeof(IReadOnlyList<CardModel>),
                typeof(EnchantmentModel),
                typeof(int),
                typeof(CardSelectorPrefs)
            });

    [HarmonyPatch]
    public static class FromDeckForEnchantmentPatch
    {
        private static System.Reflection.MethodBase TargetMethod() => FromDeckForEnchantmentTarget();

        public static void Prefix(IReadOnlyList<CardModel> cards, CardSelectorPrefs prefs, ref ExpectationState __state)
        {
            Player? player = cards.Count > 0 ? cards[0].Owner : null;
            if (player == null)
                return;
            __state = TryInstallDeckCardExpectation(player, prefs.MinSelect, prefs.MaxSelect, cards.Count, requireManualConfirmation: false);
        }

        public static void Postfix(ExpectationState __state) => Restore(__state);
    }

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

    private static ExpectationState TryInstallIndexExpectation(
        Player player,
        int minSelect,
        int maxSelect,
        int rows,
        bool requireManualConfirmation,
        bool allowNegativeIndex,
        bool allowEmptyIndexResult)
    {
        if (!CanInstallRemoteExpectation(player))
            return default;
        if (rows == 0)
            return default;
        if (!requireManualConfirmation && rows <= minSelect)
            return default;

        GridCombatMpExpectation.Active? previous = GridCombatMpExpectation.Pending.Value;
        GridCombatMpExpectation.Pending.Value = new GridCombatMpExpectation.Active
        {
            OwnerNetId = player.NetId,
            MinSelect = allowEmptyIndexResult ? 0 : minSelect,
            MaxSelect = maxSelect,
            CandidateRowCount = rows,
            AllowIndex = true,
            AllowNegativeIndex = allowNegativeIndex
        };

        GD.Print(
            $"[YgoDuelist][MP][PlayerChoice] Registered vanilla Index expectation owner={player.NetId} rows={rows} min={minSelect} max={maxSelect} allowNegative={allowNegativeIndex}");

        return new ExpectationState
        {
            Previous = previous,
            Applied = true
        };
    }

    private static ExpectationState TryInstallDeckCardExpectation(
        Player player,
        int minSelect,
        int maxSelect,
        int rows,
        bool requireManualConfirmation)
    {
        if (!CanInstallRemoteExpectation(player))
            return default;
        if (rows == 0)
            return default;
        if (!requireManualConfirmation && rows <= minSelect)
            return default;

        GridCombatMpExpectation.Active? previous = GridCombatMpExpectation.Pending.Value;
        GridCombatMpExpectation.Pending.Value = new GridCombatMpExpectation.Active
        {
            OwnerNetId = player.NetId,
            MinSelect = minSelect,
            MaxSelect = maxSelect,
            CandidateRowCount = rows,
            AllowDeckCard = true
        };

        GD.Print(
            $"[YgoDuelist][MP][PlayerChoice] Registered vanilla DeckCard expectation owner={player.NetId} rows={rows} min={minSelect} max={maxSelect}");

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

    private static bool CanInstallRemoteExpectation(Player player)
    {
        NetGameType net = RunManager.Instance.NetService.Type;
        return (net == NetGameType.Host || net == NetGameType.Client)
            && !IsLocalSelectingPlayer(player)
            && CardSelectCmd.Selector == null;
    }

    private static bool IsLocalSelectingPlayer(Player player) =>
        MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player)
        && RunManager.Instance.NetService.Type != NetGameType.Replay;
}
