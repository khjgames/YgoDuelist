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
using MegaCrit.Sts2.Core.Helpers;
using YgoDuelist.YgoDuelistCode.Cards.Command;
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

            NCardPlayQueue.Instance?.UpdateCardBeforeExecution(action);
            Creature? target = await action.Player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);

            bool needsTarget = card.TargetType == TargetType.AnyEnemy || card.TargetType == TargetType.AnyAlly;
            if (needsTarget && target == null)
            {
                GD.Print("[YgoDuelist] PlayCardFromOptionPile: card requires target but target is null (TargetId=", action.TargetId, ") - skipping play so card is not consumed");
                Log.Warn($"Attempted to play card {card} with TargetType of type 'Any', but no target was passed to the play card action!");
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
                    TaskHelper.RunSafely(changePos.OnClickedOption());
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

            // Card never leaves option pile: CardPileCmdOptionPilePlayPatch skips AddDuringManualCardPlay
            // for option-pile cards. The played card's holder was reparented for the play; its NCard can
            // stay floating. On the main thread: find and destroy that visual, then re-sync so the row rebuilds.
            var player = action.Player;
            CardModel? cardPlayed = action.NetCombatCard.ToCardModel();
            var tree = NPlayerHand.Instance?.GetTree();
            if (tree != null && player != null)
            {
                var timer = tree.CreateTimer(0.0);
                timer.Timeout += () =>
                {
                    if (IsLifecycleDebugCard(cardPlayed))
                        GD.Print("[YgoLifecycle] PlayCardFromOptionPile P1_TimerTimeout card=", cardPlayed?.GetType().Name ?? "null");

                    if (cardPlayed != null)
                    {
                        var postPlayOptionPile = player != null ? YgoCardOptionPile.CustomType.GetPile(player) : null;
                        bool cardStillInOptionPile = postPlayOptionPile != null && postPlayOptionPile.Cards.Contains(cardPlayed);
                        if (IsLifecycleDebugCard(cardPlayed))
                            GD.Print("[YgoLifecycle] PlayCardFromOptionPile P2_PostPlayPileCheck cardInOptionPile=", cardStillInOptionPile);
                        if (cardStillInOptionPile)
                        {
                            YgoOptionHandBridge.SyncFromOptionPile(player);
                            if (IsLifecycleDebugCard(cardPlayed))
                                GD.Print("[YgoLifecycle] PlayCardFromOptionPile P3_SyncedOnly_NoForcedNCardCleanup");
                            return;
                        }

                        var ncard = NCard.FindOnTable(cardPlayed);
                        if (IsLifecycleDebugCard(cardPlayed))
                            GD.Print("[YgoLifecycle] PlayCardFromOptionPile P4_FindOnTable ncardFound=", ncard != null);
                        if (ncard != null && GodotObject.IsInstanceValid(ncard))
                        {
                            ncard.Visible = false;
                            var holder = ncard.GetParent() as NHandCardHolder;
                            if (IsLifecycleDebugCard(cardPlayed))
                                GD.Print("[YgoLifecycle] PlayCardFromOptionPile P5_NCardCleanup holderFound=", holder != null);
                            if (holder != null && GodotObject.IsInstanceValid(holder))
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
                        else if (IsLifecycleDebugCard(cardPlayed))
                            GD.Print("[YgoLifecycle] PlayCardFromOptionPile P6_NoNCardFoundToCleanup");
                    }
                    YgoOptionHandBridge.SyncFromOptionPile(player);
                    if (IsLifecycleDebugCard(cardPlayed))
                        GD.Print("[YgoLifecycle] PlayCardFromOptionPile P7_FinalSyncDone");
                };
            }
        }
        finally
        {
            GD.Print("[YgoDuelist] PlayCardFromOptionPile: ExecutePlayFromOptionPileAsync END");
        }
    }
}
