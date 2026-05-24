using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
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
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Equip spells: choose a valid field monster before spending resources.
/// One legal target skips the grid; multiple targets use cancelable confirm (<see cref="YgoCancelableConfirmGridPrefs"/>).
/// Must run on <b>every</b> MP peer: <see cref="EquipSpellGridSelect"/> syncs the multi-target grid like
/// <see cref="TributeSummonGridSelect"/>; gating on <c>LocalContext.IsMe</c> left observers without
/// <see cref="EquipSpellPlayPayload"/> and caused checksum divergence on equip resolution.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(800)]
public static class PlayCardActionEquipSpellPatch
{
    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    private const string EquipSpellDefaultSelectionKey = "YGODUELIST-EQUIP_SPELL_DEFAULT.selectionScreenPrompt";

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        if (!CombatManager.Instance.IsInProgress)
            return true;

        var card = __instance.NetCombatCard.ToCardModel();
        if (card is not BaseEquipSpellCard equip || !IsEquipSpellPlayPile(equip, card.Pile))
            return true;

        __result = ExecuteWithEquipTargetSelectionAsync(__instance);
        return false;
    }

    private static bool IsEquipSpellPlayPile(BaseEquipSpellCard equip, CardPile? pile)
    {
        if (pile?.Type == PileType.Hand)
            return true;
        return pile?.Type == SpellTrapZonePile.CustomType && equip.FaceDown;
    }

    private static async Task ExecuteWithEquipTargetSelectionAsync(PlayCardAction action)
    {
        CardModel? card = null;
        try
        {
            card = action.NetCombatCard.ToCardModel();
            if (card is not BaseEquipSpellCard equip || !IsEquipSpellPlayPile(equip, card.Pile))
                return;

            var player = action.Player;
            var candidates = DuelMonsterFieldRegistry.OrderedFieldMonsters(player)
                .Where(m => YgoEquipSpellTargetRules.IsLegalEquipTarget(equip, m))
                .Cast<CardModel>()
                .ToList();

            if (candidates.Count == 0)
            {
                action.Cancel();
                return;
            }

            BaseMonsterCard? chosen;
            if (candidates.Count == 1)
            {
                chosen = candidates[0] as BaseMonsterCard;
                if (chosen == null)
                {
                    action.Cancel();
                    return;
                }

                GD.Print(
                    $"[YgoDuelist][MP][Equip] single candidate owner={player.NetId} equipIdx={action.NetCombatCard.CombatCardIndex} target={chosen.Id?.Entry}");
            }
            else
            {
                LocString prompt = ResolveEquipSelectionPrompt(equip);
                var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(prompt);
                GD.Print(
                    $"[YgoDuelist][MP][Equip] grid owner={player.NetId} equipIdx={action.NetCombatCard.CombatCardIndex} candidates={candidates.Count}");
                IEnumerable<CardModel> selected;
                try
                {
                    selected = await EquipSpellGridSelect.FromSimpleGrid(
                        YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
                        candidates,
                        player,
                        prefs);
                }
                catch (OperationCanceledException)
                {
                    action.Cancel();
                    return;
                }

                chosen = YgoMpCombatOrder.FirstCardWhereStable(selected, c => c is BaseMonsterCard) as BaseMonsterCard;
                if (chosen == null || !candidates.Contains(chosen))
                {
                    action.Cancel();
                    return;
                }
            }

            EquipSpellPlayPayload.SetPending(card, chosen);
            await ExecuteVanillaPlayCardActionBody(action);
        }
        finally
        {
            if (card != null)
                EquipSpellPlayPayload.ClearForCard(card);
        }
    }

    private static LocString ResolveEquipSelectionPrompt(BaseEquipSpellCard equip)
    {
        string cardKey = equip.Id.Entry + ".selectionScreenPrompt";
        LocString prompt = LocString.Exists("cards", cardKey)
            ? new LocString("cards", cardKey)
            : new LocString("cards", EquipSpellDefaultSelectionKey);
        equip.DynamicVars.AddTo(prompt);
        return prompt;
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
                          || pile.Type == PileType.Play);
        if (!pileOk && card is BaseEquipSpellCard eq && pile?.Type == SpellTrapZonePile.CustomType && eq.FaceDown)
            pileOk = true;
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
        {
            Log.Warn($"Attempted to play card {card} with TargetType of type 'Any', but no target was passed to the play card action!");
        }

        bool observingOtherPlayer = action.Player != null && !LocalContext.IsMe(action.Player);
        if (!observingOtherPlayer && !card.CanPlay(out _, out _))
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
