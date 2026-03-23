using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Nodes;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Monster attack / defense / hand-effect form cycle on right-click (same basis as Java; third mode when supported).
/// Uses the game's existing AltPressed signal (right-click release on card holder).
/// - Hand: we subscribe to AltPressed in AddCardHolder and toggle + refresh.
/// - Compendium/grid: we run before grid emits HolderAltPressed, toggle + refresh grid card, then detail view shows toggled form.
/// </summary>

[HarmonyPatch(typeof(NPlayerHand), "AddCardHolder")]
public static class MonsterCardHandPatch
{
    public static void Postfix(NHandCardHolder holder, int index)
    {
        // Option holders are pooled and re-added; connecting again causes "already connected". They don't need monster-form toggle.
        if (holder is NYgoOptionCardHolder)
            return;
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

[HarmonyPatch(typeof(NCardGrid), "OnHolderAltPressed")]
public static class MonsterCardGridPatch
{
    public static void Prefix(NCardHolder holder)
    {
        MonsterCardRightClickPatch.TryToggleMonsterAndRefresh(holder);
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

        // Only swap description for Attack/Defend; leave Toggle_Die_For_You, Exit_Monster_Options unchanged.
        if (card is Command_Attack && ((MonsterCommandCard)card).SourceMonster is BaseMonsterCard atkSource)
        {
            var loc = new LocString("cards", atkSource.Id.Entry + ".description_combat");
            atkSource.DynamicVars.AddTo(loc);
            return loc;
        }
        if (card is Command_Defend && ((MonsterCommandCard)card).SourceMonster is BaseMonsterCard defSource)
        {
            var loc = new LocString("cards", defSource.Id.Entry + ".description_skill_combat");
            defSource.DynamicVars.AddTo(loc);
            return loc;
        }
        return card.Description;
    }

    /// <summary>If the holder's card is a YgoDuelist monster, toggle Attack/Skill and refresh the card display.</summary>
    public static void TryToggleMonsterAndRefresh(NCardHolder holder)
    {
        if (holder.CardNode?.Model is not AbstractMonsterCard monster)
            return;

        monster.ToggleAttackSkill();

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
