using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForRestSite;

[HarmonyPatch(typeof(SmithRestSiteOption), MethodType.Constructor, typeof(Player))]
public static class YgoSmithRestSiteOptionEnabledPatch
{
    [HarmonyPostfix]
    public static void Postfix(Player owner, SmithRestSiteOption __instance)
    {
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(owner))
            return;

        CardPile? extra = PlayerRunExtraDeck.GetPileIfExists(owner);
        if (extra == null)
            return;

        if (extra.Cards.Any(c => c is FusionMonsterCard && c.IsUpgradable))
            __instance.IsEnabled = true;
    }
}

[HarmonyPatch(typeof(SmithRestSiteOption), nameof(SmithRestSiteOption.OnSelect))]
public static class YgoSmithRestSiteOptionOnSelectPatch
{
    [HarmonyPrefix]
    public static bool Prefix(SmithRestSiteOption __instance, ref Task<bool> __result)
    {
        Player owner = Traverse.Create(__instance).Field<Player>("Owner").Value;
        if (!PlayerRunExtraDeck.IsYgoDuelistPlayer(owner))
            return true;

        __result = RunYgoSmithAsync(__instance, owner);
        return false;
    }

    private static async Task<bool> RunYgoSmithAsync(SmithRestSiteOption option, Player owner)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        List<CardModel> candidates = owner.Deck.Cards.Where(c => c.IsUpgradable).ToList();
        CardPile? extra = PlayerRunExtraDeck.GetPileIfExists(owner);
        if (extra != null)
        {
            candidates.AddRange(extra.Cards.Where(c => c is FusionMonsterCard && c.IsUpgradable));
        }
        if (candidates.Count == 0)
            return false;

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), candidates, owner, prefs);
        List<CardModel> selectedList = selected.ToList();
        if (selectedList.Count == 0)
            return false;

        Traverse.Create(option).Field<IEnumerable<CardModel>>("_selection").Value = selectedList;
        foreach (CardModel picked in selectedList)
            CardCmd.Upgrade(picked, CardPreviewStyle.None);

        await Hook.AfterRestSiteSmith(owner.RunState, owner);
        return true;
    }
}
