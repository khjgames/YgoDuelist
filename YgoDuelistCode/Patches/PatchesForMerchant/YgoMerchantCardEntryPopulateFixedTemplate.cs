using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMerchant;

/// <summary>
/// YGO shop slots use a single chosen <see cref="CardModel"/> template. Vanilla
/// <see cref="CardFactory.CreateForMerchant(Player, IEnumerable{CardModel}, CardRarity)"/> filters by
/// <see cref="Hook.ModifyMerchantCardRarity"/> and existing shop canonicals (<c>Except(second)</c>), which can
/// leave zero options; <see cref="MegaCrit.Sts2.Core.Random.Rng.NextItem{T}(IEnumerable{T})"/> then returns null
/// and <see cref="RunState.CreateCard"/> throws. This path creates the offered card from the template directly.
/// </summary>
public static class YgoMerchantCardEntryPopulateFixedTemplate
{
    private static readonly MethodInfo? MerchantRollUpgrade = AccessTools.DeclaredMethod(
        typeof(CardFactory),
        "RollForUpgrade",
        new[] { typeof(Player), typeof(CardModel), typeof(decimal) });

    /// <summary>Same base chance as vanilla <see cref="CardFactory.CreateForMerchant"/> merchant path.</summary>
    private const decimal MerchantUpgradeRollBase = -999999999m;

    public static void Populate(MerchantCardEntry entry, Player player, CardModel template)
    {
        ArgumentNullException.ThrowIfNull(template);

        CardModel instance = player.RunState.CreateCard(template, player);

        if (MerchantRollUpgrade == null)
            throw new InvalidOperationException("CardFactory.RollForUpgrade(Player, CardModel, decimal) not found.");
        MerchantRollUpgrade.Invoke(null, new object[] { player, instance, MerchantUpgradeRollBase });

        var creation = new CardCreationResult(instance);
        Traverse.Create(entry).Property(nameof(MerchantCardEntry.CreationResult)).SetValue(creation);

        IRunState runState = player.RunState;
        var list = new List<CardCreationResult> { creation };
        Hook.ModifyMerchantCardCreationResults(runState, player, list);

        entry.CalcCost();
    }
}
