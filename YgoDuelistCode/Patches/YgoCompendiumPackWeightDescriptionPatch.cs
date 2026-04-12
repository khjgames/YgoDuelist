using System.Globalization;
using System.Threading;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Appends a gold PackWeightMultiplier line to card text only while compendium <see cref="NCard"/> visuals update.
/// </summary>
public static class YgoCompendiumPackWeightDescriptionPatch
{
    static readonly AsyncLocal<int> CompendiumVisualDepth = new();

    static bool IsUnderCardLibrary(NCard n)
    {
        for (Node? p = n; p != null; p = p.GetParent())
        {
            if (p is NCardLibrary)
                return true;
        }

        return false;
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    static class NCardUpdateVisualsCompendiumContext
    {
        static void Prefix(NCard __instance, ref bool __state)
        {
            __state = IsUnderCardLibrary(__instance);
            if (__state)
                CompendiumVisualDepth.Value++;
        }

        static void Postfix(bool __state)
        {
            if (__state)
                CompendiumVisualDepth.Value--;
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.GetDescriptionForPile), typeof(PileType), typeof(Creature))]
    static class AppendPackWeightLineToDescription
    {
        static void Postfix(CardModel __instance, ref string __result)
        {
            if (CompendiumVisualDepth.Value <= 0)
                return;
            if (__instance is not BaseMonsterCard bm)
                return;
            float m = bm.PackWeightMultiplier;
            if (m == 1f)
                return;
            string s = m.ToString("0.###", CultureInfo.InvariantCulture);
            __result += "\n[gold]PackWeightMultiplier : " + s + "[/gold]";
        }
    }
}
