using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NHandCardHolder), "OnMouseReleased")]
public static class SpellTrapHandHolderMouseReleasedPatch
{
    public static void Postfix(NHandCardHolder __instance, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton e || e.ButtonIndex != MouseButton.Right)
            return;

        SpellTrapCardRightClickPatch.TryToggleSpellTrapAndRefresh(__instance);
    }
}

[HarmonyPatch(typeof(NCardHolder), "OnMouseReleased")]
public static class NonHandSpellTrapHolderMouseReleasedAltPatch
{
    public static void Prefix(NCardHolder __instance, InputEvent inputEvent)
    {
        if (__instance is NHandCardHolder)
            return;

        if (!ShouldToggleSpellTrapBeforeAltPressed(__instance, inputEvent))
            return;

        SpellTrapCardRightClickPatch.TryToggleSpellTrapAndRefresh(__instance);
    }

    private static bool ShouldToggleSpellTrapBeforeAltPressed(NCardHolder holder, InputEvent inputEvent)
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

internal static class SpellTrapCardRightClickPatch
{
    private static readonly MethodInfo? NCardReload =
        typeof(NCard).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void TryToggleSpellTrapAndRefresh(NCardHolder holder)
    {
        if (holder is NHandCardHolder zoneHand
            && holder.CardNode?.Model is IYgoCardZoneRightClick zoneRc)
        {
            CardModel model = holder.CardNode.Model;
            if (MatchesRightClickActivation(model, zoneRc.RightClickActivationMask)
                && zoneRc.TryHandleCardZoneRightClick(zoneHand))
                return;
        }

        if (holder.CardNode?.Model is BaseSpellCard spell)
        {
            if (spell.Pile?.Type != PileType.Hand)
            {
                if (spell is IYgoGraveEffectDisplayForm graveNonHand
                    && graveNonHand.SupportsGraveEffectDisplayForm)
                {
                    bool allowCanonicalPreview = holder is not NHandCardHolder;
                    graveNonHand.ToggleGraveEffectDisplayForm(allowCanonicalPreview);
                    RefreshHolder(holder);
                }

                return;
            }

            if (spell.Pile?.Type == SpellTrapZonePile.CustomType)
                return;

            if (spell is IYgoGraveEffectDisplayForm graveForm
                && graveForm.SupportsGraveEffectDisplayForm)
            {
                // Regular -> Set
                if (!spell.IsSetModeInHand && !graveForm.IsGraveEffectDisplayFormActive)
                {
                    spell.ToggleSetSkillModeInHand();
                }
                // Set -> Grave Effect
                else if (spell.IsSetModeInHand)
                {
                    spell.ToggleSetSkillModeInHand();
                    graveForm.ToggleGraveEffectDisplayForm();
                }
                // Grave Effect -> Regular
                else if (graveForm.IsGraveEffectDisplayFormActive)
                {
                    graveForm.ToggleGraveEffectDisplayForm();
                }

                RefreshHolder(holder);
                return;
            }

            spell.ToggleSetSkillModeInHand();
            RefreshHolder(holder);
            return;
        }

        if (holder.CardNode?.Model is BaseTrapCard trap)
        {
            // Trap cards are always set mode in hand; no right-click toggle.
            if (trap.Pile?.Type != PileType.Hand)
                return;

            if (trap.Pile?.Type == SpellTrapZonePile.CustomType)
                return;

            RefreshHolder(holder);
        }
    }

    private static bool MatchesRightClickActivation(CardModel card, YgoCardRightClickActivation mask)
    {
        if (mask == YgoCardRightClickActivation.None)
            return false;

        bool faceUp = card is BaseSpellCard s ? !s.FaceDown : card is BaseTrapCard t && !t.FaceDown;

        if ((mask & YgoCardRightClickActivation.SpellTrapZoneFaceUp) != 0
            && card.Pile?.Type == SpellTrapZonePile.CustomType
            && faceUp)
            return true;

        if ((mask & YgoCardRightClickActivation.OptionPileFaceUp) != 0
            && card.Pile?.Type == YgoCardOptionPile.CustomType
            && faceUp)
            return true;

        return false;
    }

    internal static void RefreshHolder(NCardHolder holder)
    {
        var cardNode = holder.CardNode;

        if (holder is NHandCardHolder handHolder)
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
            return;
        }

        if (cardNode != null)
        {
            cardNode.UpdateVisuals(cardNode.DisplayingPile, CardPreviewMode.Normal);

            if (NCardReload != null)
                NCardReload.Invoke(cardNode, null);
        }
    }
}