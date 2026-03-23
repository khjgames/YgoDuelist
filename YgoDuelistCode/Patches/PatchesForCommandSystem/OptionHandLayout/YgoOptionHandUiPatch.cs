using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Nodes;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Hooks into the combat UI lifecycle and mirrors the logical YgoDuelist option
/// hand (maintained by <see cref="YgoOptionHandBridge"/>) into a concrete
/// row of NCards/NCardHolders. This is what makes the option pile behave
/// as a fully functional second hand with normal hover and targeting.
/// </summary>
[HarmonyPatch]
public static class YgoOptionHandUiPatch
{
    // Track active holders per player so we can recycle them when the
    // logical option set changes.
    private static readonly Dictionary<Player, List<NYgoOptionCardHolder>> _holdersByPlayer = new();

    private static bool _subscribed;

    /// <summary>When set, the ReturnHolderToHand patch should free this holder after returning it (option row was closed).</summary>
    public static NYgoOptionCardHolder? PendingOptionHolderToFreeAfterReturnToHand { get; set; }

    /// <summary>True if the holder's card is still in the player's option pile (e.g. cancelled targeting - don't prune).</summary>
    private static bool OptionHolderCardStillInPile(NYgoOptionCardHolder holder, Player player)
    {
        var card = holder.CardModel;
        if (card == null || player == null) return false;
        var pile = YgoCardOptionPile.CustomType.GetPile(player);
        return pile != null && pile.Cards.Contains(card);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatRoom), "_EnterTree")]
    private static void OnCombatRoomEnter(NCombatRoom __instance)
    {
        if (_subscribed)
            return;

        _subscribed = true;
        YgoSecondHandSourceBridge.SecondHandCardsChanged += OnSecondHandCardsChanged;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
    private static void OnCombatRoomExit(NCombatRoom __instance)
    {
        if (!_subscribed)
            return;

        _subscribed = false;
        YgoSecondHandSourceBridge.SecondHandCardsChanged -= OnSecondHandCardsChanged;
        ClearAll();
    }

    private static void OnSecondHandCardsChanged(Player player, IReadOnlyList<CardModel> cards)
    {
        try
        {
            GD.Print("[YgoDuelist] OnSecondHandCardsChanged ENTER player=", player?.GetHashCode() ?? 0, " cardsCount=", cards?.Count ?? 0);
            if (cards != null && cards.Count > 0)
            {
                for (int i = 0; i < cards.Count; i++)
                    GD.Print("[YgoDuelist]   cards[", i, "]=", cards[i]?.GetType().Name ?? "null", " id=", cards[i]?.GetHashCode() ?? 0);
            }
            var room = NCombatRoom.Instance;
            var ui = room?.Ui;
            if (room == null || ui == null)
            {
                GD.Print("[YgoDuelist] OnSecondHandCardsChanged EXIT room or ui null");
                return;
            }

            Player me;
            try
            {
                me = LocalContext.GetMe(player.RunState);
            }
            catch
            {
                GD.Print("[YgoDuelist] OnSecondHandCardsChanged EXIT GetMe threw");
                return;
            }

            if (me != player)
            {
                GD.Print("[YgoDuelist] OnSecondHandCardsChanged EXIT me != player");
                return;
            }

            Vector2 viewportSize = ui.GetViewportRect().Size;
            RebuildForPlayer(player, cards, ui, viewportSize);
            GD.Print("[YgoDuelist] OnSecondHandCardsChanged EXIT RebuildForPlayer done");
        }
        catch (Exception e)
        {
            YgoDuelist.MainFile.Logger.Error($"YgoOptionHandUiPatch.OnSecondHandCardsChanged error: {e}");
        }
    }

    /// <summary>Removes all tracked second-hand option holders for this player (spell/trap row, monster options, etc.).</summary>
    private static void TearDownSecondHandRow(Player player, NPlayerHand hand)
    {
        if (!_holdersByPlayer.TryGetValue(player, out var existing) || existing.Count == 0)
            return;

        var container = hand.CardHolderContainer;
        var activeOpts = hand.ActiveHolders.Where(h => h is NYgoOptionCardHolder).ToHashSet();
        var currentPlayHolder = hand.InCardPlay && AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay")?.GetValue(hand) is NCardPlay cp ? cp.Holder : null;
        bool canPrune(NYgoOptionCardHolder h) => GodotObject.IsInstanceValid(h) && !activeOpts.Contains(h) && h != currentPlayHolder && !OptionHolderCardStillInPile(h, player);
        var toFree = existing.Where(canPrune).ToList();
        int pruned = existing.RemoveAll(h => canPrune(h));
        if (pruned > 0)
        {
            GD.Print("[YgoDuelist] TearDownSecondHandRow PRUNED ", pruned, " stale holder(s)");
            foreach (var h in toFree)
            {
                if (GodotObject.IsInstanceValid(h) && h.IsInsideTree())
                    h.QueueFree();
            }
        }

        if (existing.Count == 0)
            return;

        GD.Print("[YgoDuelist] TearDownSecondHandRow REMOVING ", existing.Count, " holders");
        for (int i = 0; i < existing.Count; i++)
        {
            var h = existing[i];
            bool stillInHand = GodotObject.IsInstanceValid(h) && h.GetParent() == container;
            if (hand.FocusedHolder == h)
            {
                var focusedProp = AccessTools.Property(typeof(NPlayerHand), "FocusedHolder");
                var lastIdxField = AccessTools.Field(typeof(NPlayerHand), "_lastFocusedHolderIdx");
                focusedProp?.SetValue(hand, null);
                lastIdxField?.SetValue(hand, -1);
            }
            if (stillInHand)
                hand.RemoveCardHolder(h);
            else
            {
                bool isCurrentPlayHolder = currentPlayHolder == h;
                if (isCurrentPlayHolder)
                    PendingOptionHolderToFreeAfterReturnToHand = h;
                else if (GodotObject.IsInstanceValid(h) && h.IsInsideTree())
                    h.QueueFree();
            }
        }
        existing.Clear();
    }

    private static void RebuildForPlayer(Player player, IReadOnlyList<CardModel> cards, Node uiRoot, Vector2 viewportSize)
    {
        var hand = NPlayerHand.Instance;
        if (hand == null)
        {
            GD.Print("[YgoDuelist] RebuildForPlayer hand=null");
            return;
        }
        GD.Print("[YgoDuelist] RebuildForPlayer START handId=", hand.GetInstanceId(), " cardsCount=", cards?.Count ?? 0);

        TearDownSecondHandRow(player, hand);

        if (cards == null || cards.Count == 0)
        {
            GD.Print("[YgoDuelist] RebuildForPlayer EXIT cards null or empty (row torn down)");
            return;
        }

        if (!_holdersByPlayer.TryGetValue(player, out var existing))
        {
            existing = new List<NYgoOptionCardHolder>();
            _holdersByPlayer[player] = existing;
            GD.Print("[YgoDuelist] RebuildForPlayer new holder list for player");
        }

        PendingOptionHolderToFreeAfterReturnToHand = null;
        int count = cards.Count;
        GD.Print("[YgoDuelist] RebuildForPlayer ADDING ", count, " holders");
        for (int i = 0; i < count; i++)
        {
            var model = cards[i];
            if (model == null)
            {
                GD.Print("[YgoDuelist]   add[", i, "] SKIP model=null");
                continue;
            }
            string modelName = model.GetType().Name;
            var holder = NYgoOptionCardHolder.Create();
            GD.Print("[YgoDuelist]   add[", i, "] CREATE holderId=", holder.GetInstanceId(), " model=", modelName);
            var handField = AccessTools.Field(typeof(NHandCardHolder), "_hand");
            handField?.SetValue(holder, hand);
            YgoSecondHandHandBridge.RegisterOptionHolder(hand, holder, -1);
            GD.Print("[YgoDuelist]   add[", i, "] after RegisterOptionHolder holderId=", holder.GetInstanceId(), " IsInsideTree=", holder.IsInsideTree(), " ChildCount=", hand.GetChildCount());
            holder.Initialize(model, i, count, viewportSize);
            GD.Print("[YgoDuelist]   add[", i, "] after Initialize holderId=", holder.GetInstanceId(), " CardNode=", holder.CardNode?.GetInstanceId() ?? 0, " CardModel=", holder.CardModel?.GetType().Name ?? "null");
            existing.Add(holder);
        }
        GD.Print("[YgoDuelist] RebuildForPlayer END totalHolders=", existing.Count, " handActiveHoldersCount=", hand.ActiveHolders.Count);
        ValidateSecondHandHolders(player);
    }

    /// <summary>
    /// Run checks on option holders to detect "failed to wire" state (can't hover, dead card).
    /// Call after rebuild and optionally deferred after layout.
    /// </summary>
    public static void ValidateSecondHandHolders(Player player)
    {
        if (player == null) return;
        var hand = NPlayerHand.Instance;
        if (hand == null) return;
        if (!_holdersByPlayer.TryGetValue(player, out var holders) || holders.Count == 0)
            return;

        var active = hand.ActiveHolders;
        var activeOpts = active.Where(h => h is NYgoOptionCardHolder).ToList();
        var currentPlayHolder = hand.InCardPlay && AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay")?.GetValue(hand) is NCardPlay cp ? cp.Holder : null;
        bool canPrune(NYgoOptionCardHolder h) => GodotObject.IsInstanceValid(h) && !activeOpts.Contains(h) && h != currentPlayHolder && !OptionHolderCardStillInPile(h, player);
        var toFree = holders.Where(canPrune).ToList();
        int pruned = holders.RemoveAll(h => canPrune(h));
        if (pruned > 0)
        {
            GD.Print("[YgoDuelist] ValidateSecondHand: pruned ", pruned, " stale holder(s)");
            foreach (var h in toFree)
            {
                if (GodotObject.IsInstanceValid(h) && h.IsInsideTree())
                    h.QueueFree();
            }
        }
        if (holders.Count == 0)
            return;

        bool anyFail = false;

        for (int i = 0; i < holders.Count; i++)
        {
            var h = holders[i];
            string modelName = h.CardModel?.GetType().Name ?? "null";
            bool inTree = h.IsInsideTree();
            bool visible = h.Visible;
            bool cardOk = h.CardNode != null && h.CardNode.IsInsideTree();
            bool hitboxOk = h.Hitbox != null;
            bool hitboxVisible = hitboxOk && h.Hitbox.Visible;
            bool hitboxEnabled = hitboxOk && h.Hitbox.IsEnabled;
            bool inActive = activeOpts.Contains(h);
            bool focusStale = hand.FocusedHolder == h && !GodotObject.IsInstanceValid(h);

            if (!inTree || !visible || !cardOk || !hitboxOk || !hitboxVisible || !hitboxEnabled || !inActive || focusStale)
            {
                if (!anyFail)
                    GD.Print("[YgoDuelist] ValidateSecondHand: FAILURES for player ", player.GetHashCode(), " handId=", hand.GetInstanceId());
                anyFail = true;
                GD.Print("[YgoDuelist]   holder[", i, "] ", modelName, " id=", h.GetInstanceId(),
                    " inTree=", inTree, " visible=", visible,
                    " cardOk=", cardOk, " hitboxOk=", hitboxOk, " hitboxVisible=", hitboxVisible, " hitboxEnabled=", hitboxEnabled,
                    " inActiveHolders=", inActive, " focusStale=", focusStale);
            }
        }

        if (activeOpts.Count != holders.Count)
        {
            GD.Print("[YgoDuelist] ValidateSecondHand: active option count mismatch activeOpts=", activeOpts.Count, " tracked=", holders.Count);
            anyFail = true;
        }

        var focused = hand.FocusedHolder;
        if (focused is NYgoOptionCardHolder optFocused && !holders.Contains(optFocused))
        {
            GD.Print("[YgoDuelist] ValidateSecondHand: FocusedHolder is option holder not in our list id=", optFocused.GetInstanceId());
            anyFail = true;
        }

        if (anyFail)
            GD.Print("[YgoDuelist] ValidateSecondHand: END (had failures)");
    }

    private static void ClearAll()
    {
        foreach (var kv in _holdersByPlayer)
        {
            foreach (var h in kv.Value)
            {
                if (h.IsInsideTree())
                {
                    h.QueueFree();
                }
            }
        }

        _holdersByPlayer.Clear();
    }
}
