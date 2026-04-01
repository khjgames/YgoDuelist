using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Helpers;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When PlayCardAction runs, the game's ExecuteAction returns early if the card is not in Hand.
/// We never move option-pile cards to hand (that breaks the game). Instead we run a duplicate
/// path only when the card is in the option pile: same call site (ExecuteAction) via Harmony
/// Prefix; if card is in option pile we run ExecutePlayFromOptionPileAsync and skip the original.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
public static class PlayCardFromOptionPilePatch
{
    private static bool IsLifecycleDebugCard(CardModel? card)
    {
        var modelName = card?.GetType().Name;
        return modelName == "Activate_Effect";
    }

    /// <summary>
    /// Option-pile cards use a custom <see cref="PileType"/>; <see cref="NCard.FindOnTable"/> does not resolve them.
    /// After play, the <see cref="NCard"/> can remain under the play container while the holder is released — clean that up here.
    /// </summary>
    private static void CleanupDetachedOptionCardPlayVisual(CardModel? card)
    {
        if (card == null)
            return;

        NCard? ncard = NCombatRoom.Instance?.Ui?.GetCardFromPlayContainer(card);
        if (ncard == null || !GodotObject.IsInstanceValid(ncard))
            ncard = NCardPlayQueue.Instance?.GetCardNode(card);
        if (ncard == null || !GodotObject.IsInstanceValid(ncard))
            ncard = NPlayerHand.Instance?.GetCard(card);

        if (ncard == null || !GodotObject.IsInstanceValid(ncard))
            return;

        ncard.Visible = false;
        if (ncard.GetParent() is NHandCardHolder holder && GodotObject.IsInstanceValid(holder))
        {
            holder.Visible = false;
            if (holder.Hitbox != null)
            {
                holder.Hitbox.Visible = false;
                holder.Hitbox.SetEnabled(false);
            }
            holder.QueueFree();
        }
        else
            ncard.QueueFree();
    }

    /// <summary>
    /// Must run after option-pile <see cref="CardModel.SpendResources"/> even when <see cref="CardModel.OnPlayWrapper"/> throws;
    /// otherwise holders / play visuals are never released and the row can desync (missing command slot).
    /// </summary>
    private static void ScheduleOptionPilePostPlayCleanup(Player? player, CardModel? cardPlayed)
    {
        var tree = NPlayerHand.Instance?.GetTree();
        if (tree == null || player == null)
            return;

        var timer = tree.CreateTimer(0.0);
        timer.Timeout += () =>
        {
            if (IsLifecycleDebugCard(cardPlayed))
                GD.Print("[YgoLifecycle] PlayCardFromOptionPile P1_TimerTimeout card=", cardPlayed?.GetType().Name ?? "null");

            if (cardPlayed != null)
            {
                var postPlayOptionPile = YgoCardOptionPile.CustomType.GetPile(player);
                bool cardStillInOptionPile = postPlayOptionPile != null && postPlayOptionPile.Cards.Contains(cardPlayed);
                if (IsLifecycleDebugCard(cardPlayed))
                    GD.Print("[YgoLifecycle] PlayCardFromOptionPile P2_PostPlayPileCheck cardInOptionPile=", cardStillInOptionPile);
                if (cardStillInOptionPile)
                {
                    var hand = NPlayerHand.Instance;
                    if (hand?.GetCardHolder(cardPlayed) is NYgoOptionCardHolder optPlayed)
                        YgoOptionHandUiPatch.ForceReleaseOptionHolder(optPlayed);
                    CleanupDetachedOptionCardPlayVisual(cardPlayed);
                    YgoOptionHandBridge.ForceRefreshOptionHandFromPile(player);
                    YgoSpellTrapZoneBridge.ForceRefreshSpellTrapSecondHandFromZone(player);
                    if (IsLifecycleDebugCard(cardPlayed))
                        GD.Print("[YgoLifecycle] PlayCardFromOptionPile P3_SyncReleasedOptionHolder");
                    return;
                }

                CleanupDetachedOptionCardPlayVisual(cardPlayed);
                if (IsLifecycleDebugCard(cardPlayed))
                    GD.Print("[YgoLifecycle] PlayCardFromOptionPile P4_CleanupDetachedVisualDone");
            }

            YgoOptionHandBridge.ForceRefreshOptionHandFromPile(player);
            YgoSpellTrapZoneBridge.ForceRefreshSpellTrapSecondHandFromZone(player);
            if (IsLifecycleDebugCard(cardPlayed))
                GD.Print("[YgoLifecycle] PlayCardFromOptionPile P7_FinalSyncDone");
        };
    }

    private static readonly PropertyInfo? PlayerChoiceContextProp =
        typeof(PlayCardAction).GetProperty("PlayerChoiceContext", BindingFlags.Public | BindingFlags.Instance);

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        var player = __instance.Player;
        if (player == null)
            return true;

        var card = __instance.NetCombatCard.ToCardModel();
        if (card == null)
            return true;

        var pile = card.Pile;
        if (pile == null)
            return true;

        var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null || optionPile != pile)
            return true;

        __result = ExecutePlayFromOptionPileAsync(__instance);
        return false;
    }

    /// <summary>
    /// Duplicate of the play logic that runs only when the card is in the option pile.
    /// Does not touch the hand pile. Start/end prints for debugging.
    /// </summary>
    private static async Task ExecutePlayFromOptionPileAsync(PlayCardAction action)
    {
        GD.Print("[YgoDuelist] PlayCardFromOptionPile: ExecutePlayFromOptionPileAsync START");
        try
        {
            var card = action.NetCombatCard.ToCardModel();
            NormalMonsterCard? preplaySource = null;
            bool preparedPreplaySelection = false;
            if (card == null)
            {
                GD.Print("[YgoDuelist] PlayCardFromOptionPile: card is null, exiting");
                return;
            }
            GD.Print("[YgoDuelist] PlayCardFromOptionPile: card=", card.Id.Entry, " type=", card.GetType().Name, " TargetType=", card.TargetType);

            var pile = card.Pile;
            var optionPile = YgoCardOptionPile.CustomType.GetPile(action.Player);
            if (pile == null || optionPile == null || pile != optionPile)
            {
                GD.Print("[YgoDuelist] PlayCardFromOptionPile: card not in option pile, exiting");
                return;
            }

            if (card is Activate_Effect activate
                && activate.SourceMonster is IMonsterActivatedEffectPrePlaySelection preplay
                && activate.SourceMonster is NormalMonsterCard sourceMonster)
            {
                preplaySource = sourceMonster;
                preparedPreplaySelection = await preplay.TryPrepareActivatedEffectPlayAsync(action.Player, sourceMonster);
                if (!preparedPreplaySelection)
                {
                    action.Cancel();
                    return;
                }
            }

            NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
            Creature? target = await action.Player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);

            bool needsTarget = card.TargetType == TargetType.AnyEnemy || card.TargetType == TargetType.AnyAlly;
            if (needsTarget && target == null)
            {
                GD.Print("[YgoDuelist] PlayCardFromOptionPile: card requires target but target is null (TargetId=", action.TargetId, ") - skipping play so card is not consumed");
                Log.Warn($"Attempted to play card {card} with TargetType of type 'Any', but no target was passed to the play card action!");
                if (preparedPreplaySelection && preplaySource != null)
                    ActivatedEffectTributeSelectionPayload.ClearForSource(preplaySource);
                return;
            }

            if (!card.CanPlay(out _, out _) || !card.IsValidTarget(target))
            {
                GD.Print("[YgoDuelist] PlayCardFromOptionPile: CanPlay false or invalid target, card=", card?.Id.Entry ?? "null");
                if (card is Exit_Monster_Options exit)
                {
                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: running Exit_Monster_Options.OnClickedOption()");
                    TaskHelper.RunSafely(exit.OnClickedOption());
                }
                else if (card is Command_Change_Battle_Position changePos)
                {
                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: running Command_Change_Battle_Position.OnClickedOption()");
                    TaskHelper.RunSafely(changePos.OnClickedOption(target));
                }
                else if (card is Toggle_Die_For_You toggle)
                {
                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: running Toggle_Die_For_You.OnClickedOption()");
                    TaskHelper.RunSafely(toggle.OnClickedOption());
                }
                else
                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: card is not a known option-pile click handler, skipping OnClickedOption");
                GD.Print("[YgoDuelist] PlayCardFromOptionPile: calling action.Cancel() and returning");
                action.Cancel();
                if (preparedPreplaySelection && preplaySource != null)
                    ActivatedEffectTributeSelectionPayload.ClearForSource(preplaySource);
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
            Player? playerForCleanup = action.Player;
            CardModel? cardForCleanup = action.NetCombatCard.ToCardModel();
            try
            {
                await card.OnPlayWrapper(context, target, isAutoPlay: false, resources);
            }
            finally
            {
                // Option-pile cards are not PileType.Hand, so NCardPlayQueue never calls RemoveCardHolder — the
                // holder stays reparented under NPlayerHand with NCardPlay's bottom-screen target position.
                // If the card remains in the option pile (e.g. Command_Defend), we must free that holder before
                // SyncFromOptionPile rebuilds the row; otherwise a duplicate floats forever.
                ScheduleOptionPilePostPlayCleanup(playerForCleanup, cardForCleanup);
            }
        }
        finally
        {
            GD.Print("[YgoDuelist] PlayCardFromOptionPile: ExecutePlayFromOptionPileAsync END");
        }
    }
}
