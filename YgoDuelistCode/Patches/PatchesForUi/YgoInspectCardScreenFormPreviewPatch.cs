using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForUi;

/// <summary>
/// Close-up <see cref="NInspectCardScreen"/> previews (compendium, shop, rewards, bundles) can cycle YGO card forms
/// with right-click or controller accept on the enlarged card, matching grid and reward holders.
/// </summary>
[HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Open))]
internal static class YgoInspectCardScreenOpenFormPreviewPatch
{
    [HarmonyPostfix]
    public static void Postfix(NInspectCardScreen __instance)
    {
        YgoInspectCardScreenFormPreview.EnsureInputWiring(__instance);
    }
}

[HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")]
internal static class YgoInspectCardScreenUpdateDisplayFormSyncPatch
{
    [HarmonyPostfix]
    public static void Postfix(NInspectCardScreen __instance)
    {
        List<CardModel>? cards = YgoInspectCardScreenFormPreview.GetCards(__instance);
        CardModel? displayed = YgoInspectCardScreenFormPreview.GetCardNode(__instance).Model;
        if (cards == null || displayed == null)
            return;

        int index = YgoInspectCardScreenFormPreview.GetIndex(__instance);
        if (index < 0 || index >= cards.Count)
            return;

        YgoInspectCardScreenFormPreview.CopyYgoFormFromSource(cards[index], displayed);
        YgoInspectCardScreenFormPreview.RefreshInspectCardVisual(__instance);
    }
}

internal static class YgoInspectCardScreenFormPreview
{
    private static readonly ConditionalWeakTable<NInspectCardScreen, object> WiredScreens = new();

    private static readonly MethodInfo? NCardReload = typeof(NCard).GetMethod(
        "Reload",
        BindingFlags.NonPublic | BindingFlags.Instance);

    internal static List<CardModel>? GetCards(NInspectCardScreen screen) =>
        Traverse.Create(screen).Field<List<CardModel>?>("_cards").Value;

    internal static int GetIndex(NInspectCardScreen screen) =>
        Traverse.Create(screen).Field<int>("_index").Value;

    internal static NCard GetCardNode(NInspectCardScreen screen) =>
        Traverse.Create(screen).Field<NCard>("_card").Value;

    internal static Control GetHoverTipRect(NInspectCardScreen screen) =>
        Traverse.Create(screen).Field<Control>("_hoverTipRect").Value;

    internal static bool IsShowingUpgraded(NInspectCardScreen screen) =>
        Traverse.Create(screen).Field<NTickbox>("_upgradeTickbox").Value.IsTicked;

    public static void EnsureInputWiring(NInspectCardScreen screen)
    {
        NCard card = GetCardNode(screen);
        Control hoverTipRect = GetHoverTipRect(screen);

        // card.tscn root is mouse_filter=Ignore; HoverTipRect sits over the card and receives clicks instead.
        card.MouseFilter = Control.MouseFilterEnum.Stop;
        hoverTipRect.MouseFilter = Control.MouseFilterEnum.Stop;

        if (WiredScreens.TryGetValue(screen, out _))
            return;

        WiredScreens.Add(screen, new object());

        Callable handler = Callable.From<InputEvent>(inputEvent => HandleInspectInput(screen, inputEvent));
        card.Connect(Control.SignalName.GuiInput, handler);
        hoverTipRect.Connect(Control.SignalName.GuiInput, handler);
    }

    private static void HandleInspectInput(NInspectCardScreen screen, InputEvent inputEvent)
    {
        if (!screen.Visible)
            return;

        if (inputEvent is InputEventMouseButton mouse
            && mouse.ButtonIndex == MouseButton.Right
            && !mouse.Pressed)
        {
            if (TryCycleInspectForm(screen))
                screen.GetViewport()?.SetInputAsHandled();
            return;
        }

        if (inputEvent.IsActionReleased(MegaInput.accept) && TryCycleInspectForm(screen))
            screen.GetViewport()?.SetInputAsHandled();
    }

    public static bool TryCycleInspectForm(NInspectCardScreen screen)
    {
        List<CardModel>? cards = GetCards(screen);
        NCard cardNode = GetCardNode(screen);
        CardModel? displayed = cardNode.Model;
        if (cards == null || displayed == null)
            return false;

        int index = GetIndex(screen);
        if (index < 0 || index >= cards.Count)
            return false;

        if (!TryCycleFormOnModel(displayed))
            return false;

        CopyYgoFormToSource(cards[index], displayed);
        RefreshInspectCardVisual(screen);
        return true;
    }

    public static bool TryCycleFormOnModel(CardModel model)
    {
        if (model is AbstractMonsterCard monster)
        {
            monster.ToggleAttackSkill(allowCanonicalUiPreview: true);
            return true;
        }

        if (model is IYgoGraveEffectDisplayForm grave && grave.SupportsGraveEffectDisplayForm)
        {
            grave.ToggleGraveEffectDisplayForm(allowCanonicalUiPreview: true);
            return true;
        }

        return false;
    }

    public static void CopyYgoFormFromSource(CardModel source, CardModel displayed)
    {
        if (source is AbstractMonsterCard srcMonster && displayed is AbstractMonsterCard dispMonster)
            dispMonster.CopyDisplayFormFrom(srcMonster);

        if (source is IYgoGraveEffectDisplayForm srcGrave && displayed is IYgoGraveEffectDisplayForm dispGrave)
            dispGrave.CopyGraveEffectDisplayFormFrom(srcGrave);
    }

    public static void CopyYgoFormToSource(CardModel source, CardModel displayed)
    {
        if (source is AbstractMonsterCard srcMonster && displayed is AbstractMonsterCard dispMonster)
            srcMonster.CopyDisplayFormFrom(dispMonster);

        if (source is IYgoGraveEffectDisplayForm srcGrave && displayed is IYgoGraveEffectDisplayForm dispGrave)
            srcGrave.CopyGraveEffectDisplayFormFrom(dispGrave);
    }

    internal static void RefreshInspectCardVisual(NInspectCardScreen screen)
    {
        NCard cardNode = GetCardNode(screen);
        CardModel? model = cardNode.Model;
        if (model == null)
            return;

        if (IsShowingUpgraded(screen))
            cardNode.ShowUpgradePreview();
        else
            cardNode.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        NCardReload?.Invoke(cardNode, null);

        Control hoverTipRect = GetHoverTipRect(screen);
        NHoverTipSet.Clear();
        NHoverTipSet tipSet = NHoverTipSet.CreateAndShow(screen, model.HoverTips);
        tipSet.SetAlignment(hoverTipRect, HoverTip.GetHoverTipAlignment(screen));
    }
}
