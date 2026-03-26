using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Emergency Provisions: do a cancelable pre-play selection first.
/// Only spend resources and execute OnPlay after selection is confirmed.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(800)]
public static class PlayCardActionEmergencyProvisionsPatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        try
        {
            if (!LocalContext.IsMe(__instance.Player))
                return true;
        }
        catch
        {
            return true;
        }

        var card = __instance.NetCombatCard.ToCardModel();
        if (card is not Emergency_Provisions)
            return true;

        bool fromHand = card.Pile?.Type == PileType.Hand;
        bool fromSpellTrapZone = card.Pile?.Type == SpellTrapZonePile.CustomType;
        if (!fromHand && !fromSpellTrapZone)
            return true;

        __result = ExecuteWithSelectionAsync(__instance, card);
        return false;
    }

    private static async Task ExecuteWithSelectionAsync(PlayCardAction action, CardModel card)
    {
        try
        {
            var player = action.Player;
            var zonePile = SpellTrapZonePile.CustomType.GetPile(player);
            if (zonePile == null)
            {
                action.Cancel();
                return;
            }

            List<CardModel> candidates = zonePile.Cards
                .Where(YgoSpellTrapZoneBridge.IsSpellOrTrapCard)
                .ToList();

            if (candidates.Count == 0)
            {
                action.Cancel();
                return;
            }

            var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1, candidates.Count)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            var selection = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), candidates, player, prefs);
            var selected = selection
                .Where(c => c.Pile?.Type == SpellTrapZonePile.CustomType && YgoSpellTrapZoneBridge.IsSpellOrTrapCard(c))
                .Distinct()
                .ToList();

            if (selected.Count == 0)
            {
                action.Cancel();
                return;
            }

            EmergencyProvisionsPlayPayload.SetPending(card, selected);
            await ExecuteVanillaPlayCardActionBody(action);
        }
        finally
        {
            EmergencyProvisionsPlayPayload.ClearForCard(card);
        }
    }

    private static async Task ExecuteVanillaPlayCardActionBody(PlayCardAction action)
    {
        CardModel? card = action.NetCombatCard.ToCardModel();
        if (card == null)
            return;

        NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
        Creature? target = await action.Player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);

        CardPile? pile = card.Pile;
        if (pile == null || (pile.Type != PileType.Hand && pile.Type != SpellTrapZonePile.CustomType))
        {
            NCardPlayQueue.Instance?.RemoveCardFromQueueForCancellation(action);
            return;
        }

        bool warnMissingTarget = target == null;
        if (warnMissingTarget)
        {
            TargetType targetType = card.TargetType;
            warnMissingTarget = targetType == TargetType.AnyEnemy || targetType == TargetType.AnyAlly;
        }

        if (warnMissingTarget)
            Log.Warn($"Attempted to play card {card} with TargetType of type 'Any', but no target was passed to the play card action!");

        if (!card.CanPlay(out _, out _) || !card.IsValidTarget(target))
        {
            action.Cancel();
            return;
        }

        (int energySpent, int starsSpent) = await card.SpendResources();
        var resources = new ResourceInfo
        {
            EnergySpent = energySpent,
            EnergyValue = energySpent,
            StarsSpent = starsSpent,
            StarValue = starsSpent
        };

        var context = new GameActionPlayerChoiceContext(action);
        PlayerChoiceContextProp?.SetValue(action, context);
        await card.OnPlayWrapper(context, target, isAutoPlay: false, resources);
    }
}
