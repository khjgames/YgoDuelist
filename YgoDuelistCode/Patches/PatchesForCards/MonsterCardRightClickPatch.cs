using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Monster attack / defense / hand-effect form cycle on right-click (same basis as Java; third mode when supported).
/// Uses the game's existing AltPressed signal (right-click release on card holder, or controller inspect).
/// - Hand: <see cref="NHandCardHolder"/> overrides mouse release; we patch that and AltPressed; canonical instances stay non-toggleable there.
/// - Other holders (compendium, rewards, pack preview, grid selection): we toggle in Prefix on <see cref="NCardHolder.OnMouseReleased"/> /
///   <see cref="NCardHolder._GuiInput"/> before AltPressed, with canonical preview allowed.
/// </summary>

[HarmonyPatch(typeof(NPlayerHand), "AddCardHolder")]
public static class MonsterCardHandPatch
{
    public static void Postfix(NHandCardHolder holder, int index)
    {
        // Option holders are pooled and re-added; connecting again causes "already connected". They don't need monster-form toggle.
        if (holder is NYgoOptionCardHolder)
            return;

        // Save-load safety: normalize persisted card model state to the current hand-facing visual mode.
        // This prevents stale face-down overlays after deserialize / hand republish.
        switch (holder.CardNode?.Model)
        {
            case AbstractMonsterCard monster:
                monster.NormalizeFaceDownStateForCurrentDisplayMode();
                holder.UpdateCard();
                break;
            case BaseTrapCard trap:
                trap.NormalizeFaceDownStateForCurrentPile();
                holder.UpdateCard();
                break;
        }

        holder.Connect(NCardHolder.SignalName.AltPressed, Callable.From<NCardHolder>(OnHandHolderAltPressed));
    }

    private static void OnHandHolderAltPressed(NCardHolder holder)
    {
        if (holder is not NHandCardHolder handHolder)
            return;
        MonsterCardRightClickPatch.TryToggleMonsterAndRefresh(handHolder);
    }
}

/// <summary>
/// NHandCardHolder overrides OnMouseReleased and never calls base, so AltPressed is never emitted.
/// We handle right-click release here directly (cannot invoke base - virtual dispatch would recurse and freeze).
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "OnMouseReleased")]
public static class HandHolderMouseReleasedPatch
{
    public static void Postfix(NHandCardHolder __instance, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton e || e.ButtonIndex != MouseButton.Right)
            return;
        MonsterCardRightClickPatch.TryToggleMonsterAndRefresh(__instance);
    }
}

/// <summary>
/// Mirrors <see cref="NCardHolder.OnMouseReleased"/> conditions for the branch that emits AltPressed (right button).
/// NHandCardHolder overrides OnMouseReleased without calling base, so this never runs for the combat hand.
/// </summary>
[HarmonyPatch(typeof(NCardHolder), "OnMouseReleased")]
public static class NonHandCardHolderMouseReleasedAltPatch
{
    public static void Prefix(NCardHolder __instance, InputEvent inputEvent)
    {
        if (!ShouldToggleMonsterBeforeAltPressed(__instance, inputEvent))
            return;
        MonsterCardRightClickPatch.TryToggleMonsterAndRefresh(__instance);
    }

    private static bool ShouldToggleMonsterBeforeAltPressed(NCardHolder holder, InputEvent inputEvent)
    {
        if (holder.CardNode == null)
            return false;
        var t = Traverse.Create(holder);
        if (!t.Field<bool>("_isHovered").Value)
            return false;
        var currentPress = t.Field<InputEventMouseButton?>("_currentPressedAction").Value;
        if (currentPress == null)
            return false;
        if (!t.Field<bool>("_isClickable").Value)
            return false;
        if (inputEvent is not InputEventMouseButton emb)
            return false;
        if (emb.ButtonIndex != currentPress.ButtonIndex)
            return false;
        return emb.ButtonIndex == MouseButton.Right;
    }
}

/// <summary>
/// Controller "inspect" on card holders uses MegaInput.accept in _GuiInput, not OnMouseReleased. Toggle before AltPressed; skip hand holders
/// (they use <see cref="MonsterCardHandPatch"/> / mouse patch only for consistency with existing flow).
/// </summary>
[HarmonyPatch(typeof(NCardHolder), "_GuiInput")]
public static class NonHandCardHolderGuiInputAltPatch
{
    public static void Prefix(NCardHolder __instance, InputEvent inputEvent)
    {
        if (__instance is NHandCardHolder)
            return;
        if (!Traverse.Create(__instance).Field<bool>("_isClickable").Value)
            return;
        if (__instance.CardNode == null)
            return;
        if (!inputEvent.IsActionPressed(MegaInput.accept))
            return;
        MonsterCardRightClickPatch.TryToggleMonsterAndRefresh(__instance);
    }
}

[HarmonyPatch(typeof(CardModel))]
public static class CardModelDescriptionPatch
{
    private static MethodBase? _targetMethod;

    static MethodBase TargetMethod()
    {
        if (_targetMethod != null)
            return _targetMethod;
        var descPreviewType = typeof(CardModel).GetNestedType("DescriptionPreviewType", BindingFlags.NonPublic);
        _targetMethod = AccessTools.Method(typeof(CardModel), "GetDescriptionForPile",
            new[] { typeof(PileType), descPreviewType!, typeof(Creature) });
        return _targetMethod;
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getDescription = AccessTools.PropertyGetter(typeof(CardModel), "Description");
        var ourHelper = AccessTools.Method(typeof(MonsterCardRightClickPatch), nameof(MonsterCardRightClickPatch.GetDescriptionLocString));

        foreach (var ins in instructions)
        {
            if (ins.Calls(getDescription))
            {
                yield return new CodeInstruction(OpCodes.Call, ourHelper);
                continue;
            }
            yield return ins;
        }
    }
}

internal static class MonsterCardRightClickPatch
{
    private static readonly MethodInfo? NCardReload = typeof(NCard).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>Returns the correct description LocString so compendium and hand show attack vs skill text.</summary>
    public static LocString GetDescriptionLocString(CardModel card)
    {
        if (card is AbstractMonsterCard monster)
            return monster.GetDescriptionLocString();

        if (card is YgoDuelistCard { UsesCombatHandDescription: true } ygoHandDesc)
            return ygoHandDesc.GetCombatHandDescriptionLocString();

        if (card is MonsterCommandCard mcc)
        {
            LocString? fromCmd = mcc.GetPatchedDescriptionLocStringForDisplay();
            if (fromCmd != null)
                return fromCmd;
        }

        return card.Description;
    }

    /// <summary>If the holder's card is a YgoDuelist monster, toggle Attack/Skill and refresh the card display.</summary>
    public static void TryToggleMonsterAndRefresh(NCardHolder holder)
    {
        if (holder.CardNode?.Model is not AbstractMonsterCard monster)
            return;

        bool allowCanonicalUiPreview = holder is not NHandCardHolder;
        monster.ToggleAttackSkill(allowCanonicalUiPreview);
        if (holder is NGridCardHolder && holder.CardModel is AbstractMonsterCard backingMonster && !ReferenceEquals(backingMonster, monster))
            backingMonster.CopyDisplayFormFrom(monster);

        var cardNode = holder.CardNode;
        if (holder is NHandCardHolder handHolder)
        {
            DeferHandRefresh(handHolder, cardNode);
        }
        else if (cardNode != null)
        {
            cardNode.UpdateVisuals(cardNode.DisplayingPile, CardPreviewMode.Normal);
            if (NCardReload != null)
                NCardReload.Invoke(cardNode, null);
        }
    }

    private static void DeferHandRefresh(NHandCardHolder handHolder, NCard? cardNode)
    {
        var tree = handHolder.GetTree();
        if (tree == null)
            return;
        void OnNextFrame()
        {
            tree.ProcessFrame -= OnNextFrame;
            if (!GodotObject.IsInstanceValid(handHolder))
                return;
            handHolder.UpdateCard();
            if (cardNode != null && GodotObject.IsInstanceValid(cardNode) && NCardReload != null)
                NCardReload.Invoke(cardNode, null);
            NPlayerHand.Instance?.CallDeferred(new StringName("ForceRefreshCardIndices"));
        }
        tree.ProcessFrame += OnNextFrame;
    }
}
