using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Vanilla <c>NMouseCardPlay.StartAsync</c> only null-checks Card/CardNode once, then awaits
/// <c>StartCardDrag</c> (LerpToMouse loop). Option-hand rebuilds / pile updates during that await
/// can clear the holder's card; the next line <c>base.Card.CanPlay(...)</c> then throws
/// <see cref="NullReferenceException"/> with <c>LerpToMouse</c> still on the async stack.
/// This replays the same flow with re-validation after the drag await and a safe combat-room
/// check for the unplayable thought bubble.
/// </summary>
[HarmonyPatch(typeof(NMouseCardPlay), "StartAsync")]
public static class NMouseCardPlayStartAsyncOptionPilePatch
{
    private static bool IsLifecycleDebugHolder(NHandCardHolder? holder)
    {
        var modelName = holder?.CardModel?.GetType().Name;
        return holder is NYgoOptionCardHolder || modelName == "Activate_Effect";
    }

    private static bool SafeInTree(Node? node)
    {
        if (node == null)
            return false;
        try
        {
            return GodotObject.IsInstanceValid(node) && node.IsInsideTree();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private static void LogLifecycle(string point, NMouseCardPlay self, string extra = "")
    {
        try
        {
            var holder = self?.Holder;
            if (!IsLifecycleDebugHolder(holder))
                return;
            GD.Print("[YgoLifecycle] MouseStartAsync ", point,
                " holderId=", holder?.GetInstanceId() ?? 0,
                " model=", holder?.CardModel?.GetType().Name ?? "null",
                " holderInTree=", SafeInTree(holder),
                " selfInTree=", SafeInTree(self),
                " extra=", extra);
        }
        catch (ObjectDisposedException)
        {
            // self/holder may be freed after TargetSelection; avoid crashing the async continuation
        }
    }

    private static readonly MethodInfo StartCardDragMethod =
        AccessTools.DeclaredMethod(typeof(NMouseCardPlay), "StartCardDrag")!;

    private static readonly MethodInfo TargetSelectionMethod =
        AccessTools.DeclaredMethod(typeof(NMouseCardPlay), "TargetSelection", new[] { typeof(TargetMode) })!;

    private static readonly MethodInfo IsCardInPlayZoneMethod =
        AccessTools.DeclaredMethod(typeof(NMouseCardPlay), "IsCardInPlayZone")!;

    private static readonly MethodInfo TryPlayCardMethod =
        AccessTools.DeclaredMethod(typeof(NCardPlay), "TryPlayCard", new[] { typeof(Creature) })!;

    private static readonly MethodInfo CannotPlayFtueMethod =
        AccessTools.DeclaredMethod(typeof(NCardPlay), "CannotPlayThisCardFtueCheck", new[] { typeof(CardModel) })!;

    private static readonly FieldInfo CtsField =
        AccessTools.Field(typeof(NMouseCardPlay), "_cancellationTokenSource")!;

    private static readonly FieldInfo SkipStartCardDragField =
        AccessTools.Field(typeof(NMouseCardPlay), "_skipStartCardDrag")!;

    private static readonly FieldInfo IsLeftMouseDownField =
        AccessTools.Field(typeof(NMouseCardPlay), "_isLeftMouseDown")!;

    private static readonly FieldInfo TargetField =
        AccessTools.Field(typeof(NMouseCardPlay), "_target")!;

    private static readonly PropertyInfo CardProperty =
        AccessTools.Property(typeof(NCardPlay), "Card")!;

    private static readonly PropertyInfo CardNodeProperty =
        AccessTools.Property(typeof(NCardPlay), "CardNode")!;

    private static readonly MethodInfo? UnplayableDialogueMethod =
        typeof(UnplayableReason).Assembly
            .GetType("MegaCrit.Sts2.Core.Entities.Cards.UnplayableReasonExtensions")?
            .GetMethod(
                "GetPlayerDialogueLine",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(UnplayableReason), typeof(AbstractModel) },
                null);

    static bool Prefix(NMouseCardPlay __instance, ref Task __result)
    {
        __result = RunStartAsync(__instance);
        return false;
    }

    private static async Task RunStartAsync(NMouseCardPlay self)
    {
        var holder = self.Holder;
        LogLifecycle("M1_Enter", self);
        if (holder is NYgoOptionCardHolder opt)
        {
            if (!SafeInTree(opt))
            {
                LogLifecycle("M2_OptionHolderInvalid_Cancel", self);
                self.CancelPlayCard();
                return;
            }
        }

        if (CardOf(self) == null || CardNodeOf(self) == null)
        {
            LogLifecycle("M3_CardOrNodeNull_BeforeDrag", self);
            return;
        }

        LogLifecycle("M4_StartCardDrag_BeginAwait", self);
        await (Task)StartCardDragMethod.Invoke(self, null)!;
        LogLifecycle("M5_StartCardDrag_EndAwait", self);

        if (!SafeInTree(self))
        {
            LogLifecycle("M6_SelfInvalid_AfterDrag", self);
            return;
        }

        if (IsCtsCancelled(self))
        {
            LogLifecycle("M7_CtsCancelled_AfterDrag", self);
            return;
        }

        var card = CardOf(self);
        var cardNode = CardNodeOf(self);
        if (card == null || cardNode == null)
        {
            LogLifecycle("M8_CardOrNodeNull_AfterDrag_Cancel", self);
            self.CancelPlayCard();
            return;
        }

        if (!card.CanPlay(out UnplayableReason reason, out AbstractModel preventer))
        {
            LogLifecycle("M9_CannotPlay_Cancel", self, $"reason={reason}");
            CannotPlayFtueMethod.Invoke(self, new object[] { card });
            self.CancelPlayCard();
            LocString? line = UnplayableDialogueMethod?.Invoke(null, new object?[] { reason, preventer }) as LocString;
            if (line != null)
            {
                var room = NCombatRoom.Instance;
                if (room != null)
                {
                    room.CombatVfxContainer.AddChildSafely(
                        NThoughtBubbleVfx.Create(line.GetFormattedText(), card.Owner.Creature, 1.0));
                }
            }
            return;
        }

        cardNode.CardHighlight.AnimFlash();
        bool skipDrag = (bool)(SkipStartCardDragField.GetValue(self) ?? false);
        bool leftDown = (bool)(IsLeftMouseDownField.GetValue(self) ?? false);
        TargetMode targetMode = !skipDrag
            ? (leftDown ? TargetMode.ReleaseMouseToTarget : TargetMode.ClickMouseToTarget)
            : TargetMode.ClickMouseToTarget;

        await (Task)TargetSelectionMethod.Invoke(self, new object[] { targetMode })!;
        LogLifecycle("M10_TargetSelectionCompleted", self, $"targetMode={targetMode}");

        if (IsCtsCancelled(self))
        {
            LogLifecycle("M11_CtsCancelled_AfterTargetSelection", self);
            return;
        }

        if (!(bool)IsCardInPlayZoneMethod.Invoke(self, null)!)
        {
            LogLifecycle("M12_NotInPlayZone_Cancel", self);
            self.CancelPlayCard();
        }

        if (!IsCtsCancelled(self))
        {
            LogLifecycle("M13_InvokeTryPlayCard", self);
            TryPlayCardMethod.Invoke(self, new object?[] { TargetField.GetValue(self) as Creature });
        }
    }

    private static CardModel? CardOf(NCardPlay p) => CardProperty.GetValue(p) as CardModel;

    private static NCard? CardNodeOf(NCardPlay p) => CardNodeProperty.GetValue(p) as NCard;

    private static bool IsCtsCancelled(NMouseCardPlay self)
    {
        var cts = CtsField.GetValue(self) as CancellationTokenSource;
        if (cts == null)
            return true;
        try
        {
            return cts.IsCancellationRequested;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }
}
