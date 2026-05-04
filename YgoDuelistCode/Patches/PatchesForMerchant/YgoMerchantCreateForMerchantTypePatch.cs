using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelistCharacter = YgoDuelist.YgoDuelistCode.Character.YgoDuelist;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Merchant colored slots use Attack / Skill / Power. YgoDuelist cards are almost all <see cref="CardType.Skill"/>,
/// so vanilla shop generation exhausts rarities and throws. When that would happen, pick by rarity only (ignore card type).
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForMerchant), typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType))]
public static class YgoMerchantCreateForMerchantTypePatch
{
    private static readonly MethodInfo RollForUpgradeMethod =
        AccessTools.DeclaredMethod(typeof(CardFactory), "RollForUpgrade", new[] { typeof(Player), typeof(CardModel), typeof(decimal) })!;

    [HarmonyPrefix]
    public static bool Prefix(Player player, IEnumerable<CardModel> options, CardType type, ref CardCreationResult __result)
    {
        if (player.Character is not YgoDuelistCharacter)
            return true;

        if (player.Character is Deprived)
            throw new System.InvalidOperationException("Merchant inventory can't be generated for the test character. Update your test to use Ironclad.");

        options = Hook.ModifyMerchantCardPool(player.RunState, player, options);
        options = options.Where(c => c.Rarity != CardRarity.Basic);
        options = FilterForPlayerCount(player.RunState, options);
        CardModel[] source = options.ToArray();

        CardRarity rolledRarity = Hook.ModifyMerchantCardRarity(
            player.RunState,
            player,
            player.PlayerOdds.CardRarity.RollWithoutChangingFutureOdds(CardRarityOddsType.Shop));

        CardRarity startRarity = rolledRarity;
        List<CardModel> list = source.Where(c => c.Rarity == rolledRarity && c.Type == type).ToList();
        while (list.Count == 0)
        {
            rolledRarity = rolledRarity.GetNextHighestRarity();
            if (rolledRarity == CardRarity.None)
            {
                rolledRarity = startRarity;
                while (true)
                {
                    list = source.Where(c => c.Rarity == rolledRarity).ToList();
                    if (list.Count > 0)
                        break;
                    rolledRarity = rolledRarity.GetNextHighestRarity();
                    if (rolledRarity == CardRarity.None)
                    {
                        list = source.ToList();
                        break;
                    }
                }

                break;
            }

            list = source.Where(c => c.Rarity == rolledRarity && c.Type == type).ToList();
        }

        if (list.Count == 0)
            throw new System.InvalidOperationException("Can't generate a valid merchant card for YgoDuelist (empty card pool after filters).");

        CardModel pick = player.PlayerRng.Shops.NextItem(list)
            ?? throw new System.InvalidOperationException("Shop RNG returned no card from non-empty list.");
        CardModel cardModel = player.RunState.CreateCard(pick, player);
        RollForUpgradeMethod.Invoke(null, new object[] { player, cardModel, -999999999m });
        __result = new CardCreationResult(cardModel);
        return false;
    }

    private static IEnumerable<CardModel> FilterForPlayerCount(IRunState runState, IEnumerable<CardModel> opts)
    {
        IEnumerable<CardModel> filtered = runState.Players.Count > 1
            ? opts.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly)
            : opts.Where(c => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);
        return filtered.Where(c => !YgoPackCardCatalog.IsYgoBlockedFromMultiplayerProceduralPools(runState, c));
    }
}
