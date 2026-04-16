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

[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(800)]
public static class PlayCardActionRushReliablePatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        var card = __instance.NetCombatCard.ToCardModel();
        if (card is not Rush_Recklessly && card is not The_Reliable_Guardian)
            return true;

        bool fromHand = card.Pile?.Type == PileType.Hand;
        bool fromSpellTrapZone = card.Pile?.Type == SpellTrapZonePile.CustomType;
        bool fromOptionPile = card.Pile?.Type == YgoCardOptionPile.CustomType;
        if (!fromHand && !fromSpellTrapZone && !fromOptionPile)
            return true;

        __result = ExecuteWithSelectionAsync(__instance, card);
        return false;
    }

    private static async Task ExecuteWithSelectionAsync(PlayCardAction action, CardModel card)
    {
        try
        {
            var player = action.Player;
            var candidates = DuelMonsterFieldRegistry.GetFieldMonsters(player)
                .OfType<BaseMonsterCard>()
                .ToList();
            if (candidates.Count == 0)
            {
                action.Cancel();
                return;
            }

            var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            var pick = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), candidates, player, prefs);
            var selected = pick.OfType<BaseMonsterCard>().FirstOrDefault();
            if (selected == null)
            {
                action.Cancel();
                return;
            }

            RushReliablePlayPayload.SetPending(card, selected);
            await ExecuteVanillaPlayCardActionBody(action);
        }
        finally
        {
            RushReliablePlayPayload.ClearForCard(card);
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
