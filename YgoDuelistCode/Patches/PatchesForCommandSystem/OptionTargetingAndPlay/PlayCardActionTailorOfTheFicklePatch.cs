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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Tailor of the Fickle: always runs two explicit, cancelable prompts before spending resources:
/// 1) choose an equipped equip spell that has at least one alternate valid monster target
/// 2) choose the new valid monster target for that equip spell
/// Canceling either prompt aborts the play.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(800)]
public static class PlayCardActionTailorOfTheFicklePatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        var card = __instance.NetCombatCard.ToCardModel();
        if (card is not Tailor_of_the_Fickle tailor)
            return true;

        bool fromHand = card.Pile?.Type == PileType.Hand;
        bool fromSpellTrapZone = card.Pile?.Type == SpellTrapZonePile.CustomType;
        bool fromOptionPile = card.Pile?.Type == YgoCardOptionPile.CustomType;
        if (!fromHand && !fromSpellTrapZone && !fromOptionPile)
            return true;

        __result = ExecuteWithSelectionsAsync(__instance, tailor);
        return false;
    }

    private static async Task ExecuteWithSelectionsAsync(PlayCardAction action, Tailor_of_the_Fickle tailor)
    {
        CardModel card = tailor;
        try
        {
            var player = action.Player;
            var equipCandidates = Tailor_of_the_Fickle.GetReassignableEquips(player).Cast<CardModel>().ToList();
            if (equipCandidates.Count == 0)
            {
                action.Cancel();
                return;
            }

            var equipPrefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            var equipPick = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), equipCandidates, player, equipPrefs);
            var selectedEquip = equipPick.FirstOrDefault() as BaseEquipSpellCard;
            if (selectedEquip == null)
            {
                action.Cancel();
                return;
            }

            var targetCandidates = Tailor_of_the_Fickle.GetAlternateValidTargets(selectedEquip, player).Cast<CardModel>().ToList();
            if (targetCandidates.Count == 0)
            {
                action.Cancel();
                return;
            }

            var targetPrefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            var targetPick = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), targetCandidates, player, targetPrefs);
            var selectedTarget = targetPick.FirstOrDefault() as BaseMonsterCard;
            if (selectedTarget == null)
            {
                action.Cancel();
                return;
            }

            TailorOfTheFicklePlayPayload.SetPending(card, new TailorOfTheFicklePendingResolution(selectedEquip, selectedTarget));
            await ExecuteVanillaPlayCardActionBody(action);
        }
        finally
        {
            TailorOfTheFicklePlayPayload.ClearForCard(card);
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
        bool pileOk = pile != null
            && (pile.Type == PileType.Hand
                || pile.Type == SpellTrapZonePile.CustomType
                || pile.Type == YgoCardOptionPile.CustomType);
        if (!pileOk)
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

        bool observingOtherPlayer = action.Player != null && !LocalContext.IsMe(action.Player);
        if (!observingOtherPlayer && (!card.CanPlay(out _, out _) || !card.IsValidTarget(target)))
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
