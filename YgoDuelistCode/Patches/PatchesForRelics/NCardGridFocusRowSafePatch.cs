using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Vanilla <see cref="NCardGrid"/> indexes <c>_cardRows[0][0]</c> whenever <c>_cards.Count != 0</c>, but rows are filled asynchronously after <see cref="NCardGrid.SetCards"/>.
/// That throws during overlay navigation; return null until the first row exists.
/// </summary>
[HarmonyPatch(typeof(NCardGrid), nameof(NCardGrid.FocusedControlFromTopBar), MethodType.Getter)]
public static class NCardGridFocusedControlFromTopBarSafePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NCardGrid __instance, ref Control? __result)
    {
        List<CardModel> cards = Traverse.Create(__instance).Field<List<CardModel>>("_cards").Value;
        List<List<NGridCardHolder>> rows = Traverse.Create(__instance).Field<List<List<NGridCardHolder>>>("_cardRows").Value;
        if (cards.Count == 0)
            return true;
        if (rows.Count == 0 || rows[0].Count == 0)
        {
            __result = null;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(NCardGrid), nameof(NCardGrid.DefaultFocusedControl), MethodType.Getter)]
public static class NCardGridDefaultFocusedControlSafePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NCardGrid __instance, ref Control? __result)
    {
        List<CardModel> cards = Traverse.Create(__instance).Field<List<CardModel>>("_cards").Value;
        List<List<NGridCardHolder>> rows = Traverse.Create(__instance).Field<List<List<NGridCardHolder>>>("_cardRows").Value;
        if (Traverse.Create(__instance).Field<NCardHolder?>("_lastFocusedHolder").Value != null)
            return true;
        if (cards.Count == 0)
            return true;
        if (rows.Count == 0 || rows[0].Count == 0)
        {
            __result = null;
            return false;
        }

        return true;
    }
}
