using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Combat play phase: cards implementing <see cref="IYgoNHandPlayPhaseHighlightOverride"/> may replace the vanilla cyan
/// playable outline (e.g. Fusion Gate and Egyptian God Slime command — purple when their play path is active).
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "UpdateCard")]
public static class YgoFusionGateFieldGlowPatch
{
    private static readonly PropertyInfo? ShouldGlowGoldProp =
        AccessTools.Property(typeof(NHandCardHolder), "ShouldGlowGold");

    /// <summary>
    /// <see cref="NHandCardHolder.Create"/> calls <see cref="NHandCardHolder.SetCard"/> before assigning <c>_hand</c>;
    /// the private <c>ShouldGlowGold</c> getter dereferences <c>_hand</c> and must not be invoked until it is set.
    /// </summary>
    private static readonly FieldInfo? HolderHandField =
        AccessTools.Field(typeof(NHandCardHolder), "_hand");

    static void Postfix(NHandCardHolder __instance)
    {
        if (!CombatManager.Instance.IsInProgress || !CombatManager.Instance.IsPlayPhase)
            return;

        if (HolderHandField?.GetValue(__instance) == null)
            return;

        CardModel? model = __instance.CardNode?.Model;
        if (model is not IYgoNHandPlayPhaseHighlightOverride hl)
            return;

        bool cyanPlayable = WouldVanillaUsePlayableCyanHighlight(__instance, model);
        Color? modulate = hl.GetNHandPlayPhaseHighlightModulateOverride(__instance, cyanPlayable);
        if (!modulate.HasValue)
            return;

        ApplyHighlightModulate(__instance, modulate.Value);
    }

    private static void ApplyHighlightModulate(NHandCardHolder holder, Color modulate)
    {
        NCard? node = holder.CardNode;
        if (node == null)
            return;

        node.CardHighlight.AnimShow();
        node.CardHighlight.Modulate = modulate;
    }

    /// <summary>
    /// True when <see cref="NHandCardHolder.UpdateCard"/> would set <see cref="NCardHighlight.playableColor"/> (cyan),
    /// i.e. the card is in the playable branch and is not using red or gold glow.
    /// </summary>
    private static bool WouldVanillaUsePlayableCyanHighlight(NHandCardHolder holder, CardModel card)
    {
        if (card.ShouldGlowRed)
            return false;

        if (SafeShouldGlowGold(holder))
            return false;

        return card.CanPlay();
    }

    private static bool SafeShouldGlowGold(NHandCardHolder holder)
    {
        try
        {
            return ShouldGlowGoldProp?.GetValue(holder) is true;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is NullReferenceException)
        {
            return false;
        }
    }
}
