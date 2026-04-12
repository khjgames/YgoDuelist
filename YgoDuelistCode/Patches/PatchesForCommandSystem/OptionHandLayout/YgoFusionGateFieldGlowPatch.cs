using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Fusion Gate: replace the cyan playable outline with purple when a Fusion Summon is legal and vanilla would have
/// shown cyan (not red/gold). Face-down set Field Spells in the zone use that path when <see cref="CardModel.CanPlay"/>.
/// Face-up on the field is not <c>CanPlay</c> in vanilla (no cyan there); we still show purple when a zone fusion is legal
/// so the right-click prompt stays visible.
/// <para><see cref="Special_Summon_Egyptian_God_Slime"/> in the monster-options row uses the same purple when playable.</para>
/// </summary>
[HarmonyPatch(typeof(NHandCardHolder), "UpdateCard")]
public static class YgoFusionGateFieldGlowPatch
{
    private static readonly Color FusionGatePurple = new(0.78f, 0.42f, 1f, 0.98f);

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
        if (model is Fusion_Gate gate)
        {
            PileType pileType = gate.Pile?.Type ?? PileType.None;
            if (pileType != SpellTrapZonePile.CustomType && pileType != PileType.Hand)
                return;

            Player? owner = gate.Owner;
            if (owner == null)
                return;

            try
            {
                if (!LocalContext.IsMe(owner))
                    return;
            }
            catch
            {
                return;
            }

            if (!FusionSummonSelection.HasFeasibleFusionPlay(owner, gate))
                return;

            bool cyanPlayable = WouldVanillaUsePlayableCyanHighlight(__instance, gate);
            bool faceUpInZone = pileType == SpellTrapZonePile.CustomType && !gate.FaceDown;
            if (!cyanPlayable && !faceUpInZone)
                return;

            ApplyPurpleHighlight(__instance);
            return;
        }

        if (model is Special_Summon_Egyptian_God_Slime slimeCmd)
        {
            PileType pileType = slimeCmd.Pile?.Type ?? PileType.None;
            if (pileType != YgoCardOptionPile.CustomType)
                return;

            Player? owner = slimeCmd.Owner;
            if (owner == null)
                return;

            try
            {
                if (!LocalContext.IsMe(owner))
                    return;
            }
            catch
            {
                return;
            }

            if (!WouldVanillaUsePlayableCyanHighlight(__instance, slimeCmd))
                return;

            ApplyPurpleHighlight(__instance);
        }
    }

    private static void ApplyPurpleHighlight(NHandCardHolder holder)
    {
        NCard? node = holder.CardNode;
        if (node == null)
            return;

        node.CardHighlight.AnimShow();
        node.CardHighlight.Modulate = FusionGatePurple;
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
