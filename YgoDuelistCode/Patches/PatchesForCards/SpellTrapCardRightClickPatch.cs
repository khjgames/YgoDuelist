using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
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

internal static class SpellTrapCardRightClickPatch
{
    private static readonly MethodInfo? NCardReload = typeof(NCard).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void TryToggleSpellTrapAndRefresh(NCardHolder holder)
    {
        if (holder.CardNode?.Model is BaseSpellCard spell)
        {
            if (spell.Pile?.Type != PileType.Hand)
                return;
            if (spell.Pile?.Type == SpellTrapZonePile.CustomType)
                return;
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

    private static void RefreshHolder(NCardHolder holder)
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
