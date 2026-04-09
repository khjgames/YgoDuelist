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
[HarmonyPriority(798)]
public static class PlayCardActionSecretPassPatch
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
        if (card is not Secret_Pass_to_the_Treasures sp)
            return true;

        bool fromHand = card.Pile?.Type == PileType.Hand;
        bool fromSpellTrapZone = card.Pile?.Type == SpellTrapZonePile.CustomType;
        if (!fromHand && !fromSpellTrapZone)
            return true;

        __result = ExecuteWithSelectionAsync(__instance, card, sp);
        return false;
    }

    private static async Task ExecuteWithSelectionAsync(PlayCardAction action, CardModel card, Secret_Pass_to_the_Treasures spell)
    {
        try
        {
            var player = action.Player;
            decimal maxAtk = spell.AtkThresholdForSelection;
            var field = DuelMonsterFieldRegistry.GetFieldMonsters(player).OfType<BaseMonsterCard>().ToList();
            var candidates = field
                .Where(m => m.CalcDuelMonsterStats(field).Atk <= maxAtk)
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

            SecretPassPlayPayload.SetPending(card, selected);
            await ExecuteVanillaPlayCardActionBody(action);
        }
        finally
        {
            SecretPassPlayPayload.ClearForCard(card);
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
