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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(799)]
public static class PlayCardActionRiryokuPatch
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
        if (card is not Riryoku)
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
            if (candidates.Count < 2)
            {
                action.Cancel();
                return;
            }

            var prefs1 = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            var pick1 = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), candidates, player, prefs1);
            var donor = pick1.OfType<BaseMonsterCard>().FirstOrDefault();
            if (donor == null)
            {
                action.Cancel();
                return;
            }

            var secondList = candidates.Where(c => !ReferenceEquals(c, donor)).ToList();
            var prefs2 = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            var pick2 = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), secondList, player, prefs2);
            var receiver = pick2.OfType<BaseMonsterCard>().FirstOrDefault();
            if (receiver == null)
            {
                action.Cancel();
                return;
            }

            RiryokuPlayPayload.SetPending(card, donor, receiver);
            await ExecuteVanillaPlayCardActionBody(action);
        }
        finally
        {
            RiryokuPlayPayload.ClearForCard(card);
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
