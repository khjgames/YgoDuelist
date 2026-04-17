using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
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
using YgoDuelist.YgoDuelistCode.GameActions;
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
    private static bool IsLifecycleDebugCard(CardModel? card) =>
        card is MonsterCommandCard mcc && mcc.LogsOptionPileLifecycle;

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
    /// <summary>
    /// Ritual/fusion spells from the option row are executed by <see cref="PlayCardActionRitualSpellPatch"/> /
    /// <see cref="PlayCardActionFusionSpellPatch"/> (they run before this patch). Call this after their
    /// <c>OnPlayWrapper</c> so holders and second-hand UI match the option-pile path.
    /// </summary>
    internal static void SchedulePostPlayCleanup(Player? player, CardModel? cardPlayed) =>
        ScheduleOptionPilePostPlayCleanup(player, cardPlayed);

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

    /// <summary>
    /// MP: <see cref="CardModel.Pile"/> on the instance returned from <see cref="NetCombatCard.ToCardModel"/> can be null
    /// or not reference-equal to <see cref="YgoCardOptionPile"/> on observers even when the card is in that pile — vanilla
    /// then skips this patch and combat never mirrors (checksum divergence after e.g. <see cref="Command_Attack"/>).
    /// </summary>
    private static CardModel? TryResolveCanonicalOptionPileCard(Player player, CardModel card)
    {
        CardPile? optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null)
            return null;

        if (optionPile.Cards.Contains(card))
            return card;

        YgoNetCombatCardPileGate.EnsureMutableCombatCardsHaveNetIds(new[] { card });
        YgoNetCombatCardPileGate.EnsureMutableCombatCardsHaveNetIds(optionPile.Cards);

        uint id = NetCombatCardDb.Instance.GetCardId(card);
        return optionPile.Cards.FirstOrDefault(c => NetCombatCardDb.Instance.GetCardId(c) == id);
    }

    private static bool IsOptionPilePlay(Player player, CardModel card)
    {
        CardPile? optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null)
            return false;

        if (card.Pile != null && ReferenceEquals(card.Pile, optionPile))
            return true;

        return TryResolveCanonicalOptionPileCard(player, card) != null;
    }

    static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        var player = __instance.Player;
        if (player == null)
            return true;

        var card = __instance.NetCombatCard.ToCardModel();
        if (card == null)
            return true;
        if (card is MonsterCommandCard mccPrefix)
            mccPrefix.TryResolveSourceMonsterFromStoredPetId();

        if (!IsOptionPilePlay(player, card))
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
                GD.PrintErr("[YgoDuelist][MP][OptionPile] card is null after NetCombatCard.ToCardModel — Cancel");
                action.Cancel();
                return;
            }

            CardModel? canonical = TryResolveCanonicalOptionPileCard(action.Player, card);
            if (canonical != null && !ReferenceEquals(canonical, card))
            {
                GD.Print(
                    $"[YgoDuelist][MP][OptionPile] canonical pile instance (was null/mismatched Pile) netId={NetCombatCardDb.Instance.GetCardId(card)} entry={card.Id?.Entry} owner={action.Player?.NetId}");
                card = canonical;
            }

            GD.Print("[YgoDuelist] PlayCardFromOptionPile: card=", card.Id.Entry, " type=", card.GetType().Name, " TargetType=", card.TargetType);

            if (card is MonsterCommandCard mccPlay)
                mccPlay.TryResolveSourceMonsterFromStoredPetId();

            var optionPile = YgoCardOptionPile.CustomType.GetPile(action.Player);
            if (optionPile == null || !optionPile.Cards.Contains(card))
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP][OptionPile] card not in option pile before execute — Cancel (pileNull={optionPile == null} owner={action.Player?.NetId})");
                action.Cancel();
                return;
            }

            if (card is MonsterCommandCard mccNeedSource && mccNeedSource.SourceMonster == null)
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP][OptionPile] MonsterCommandCard SourceMonster null after TryResolve; card={card.Id.Entry} SourcePetCombatId={mccNeedSource.SourcePetCombatId} owner={action.Player?.NetId}");
                action.Cancel();
                return;
            }

            if (card is IActivateEffectPrePlayOptionPileCommand
                && card is MonsterCommandCard activate
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

            if (card is MonsterCommandCard mccResolved)
            {
                GD.Print(
                    $"[YgoDuelist][MP][OptionPile] post-target-resolve card={card.Id.Entry} source={mccResolved.SourceMonster?.Id.Entry} sourcePetId={mccResolved.SourcePetCombatId} actionTargetId={action.TargetId} resolvedTargetCombatId={target?.CombatId}");
            }

            bool needsTarget = card.TargetType == TargetType.AnyEnemy || card.TargetType == TargetType.AnyAlly;
            if (needsTarget && target == null)
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP][OptionPile] AnyEnemy/AnyAlly requires target but GetCreatureAsync returned null (TargetId={action.TargetId}) — Cancel so queue/state match peers");
                Log.Warn($"Attempted to play card {card} with TargetType of type 'Any', but no target was passed to the play card action!");
                if (preparedPreplaySelection && preplaySource != null)
                {
                    ActivatedEffectTributeSelectionPayload.ClearForSource(preplaySource);
                    ObeliskActivatedTributePayload.ClearForSource(preplaySource);
                }
                action.Cancel();
                return;
            }

            bool observingOtherPlayer = action.Player != null && !LocalContext.IsMe(action.Player);
            if (!observingOtherPlayer
                && (!card.CanPlay(out UnplayableReason unplayable, out AbstractModel? _) || !card.IsValidTarget(target)))
            {
                GD.Print(
                    $"[YgoDuelist][MP][OptionPile] unplayable or invalid target card={card?.Id.Entry} unplayable={unplayable} targetCombat={target?.CombatId}");
                GD.Print("[YgoDuelist] PlayCardFromOptionPile: CanPlay false or invalid target, card=", card?.Id.Entry ?? "null");
                if (card is MonsterCommandCard mccMenu && mccMenu.TryEnqueueUnplayableOptionPileMenu(action.Player!, target))
                {
                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: unplayable menu card — YgoMonsterMenuCommandNetHelper");
                }
                else
                    GD.Print("[YgoDuelist] PlayCardFromOptionPile: card is not a known option-pile click handler, skipping OnClickedOption");
                GD.Print("[YgoDuelist] PlayCardFromOptionPile: calling action.Cancel() and returning");
                action.Cancel();
                if (preparedPreplaySelection && preplaySource != null)
                {
                    ActivatedEffectTributeSelectionPayload.ClearForSource(preplaySource);
                    ObeliskActivatedTributePayload.ClearForSource(preplaySource);
                }
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
