using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// When <c>cards/{Id}.selectionScreenPrompt</c> is missing, use a dev-facing default instead of throwing.
/// </summary>
[HarmonyPatch(typeof(CardModel), "SelectionScreenPrompt", MethodType.Getter)]
public static class CardModelSelectionScreenPromptFallbackPatch
{
    private const string MissingPromptKey = "YGODUELIST_INTERNAL.missing_selection_prompt";

    private const string MissingPromptTemplate =
        "[ERROR] {ClassName} Some moron forgot a selection prompt Localization for this card.";

    private static bool _templateMerged;

    private static void EnsureMissingPromptTemplateInCardsTable()
    {
        if (_templateMerged)
            return;
        LocManager.Instance.GetTable("cards").MergeWith(new Dictionary<string, string>
        {
            { MissingPromptKey, MissingPromptTemplate }
        });
        _templateMerged = true;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    static bool Prefix(CardModel __instance, ref LocString __result)
    {
        var key = __instance.Id.Entry + ".selectionScreenPrompt";
        if (LocString.Exists("cards", key))
            return true;

        EnsureMissingPromptTemplateInCardsTable();

        __result = new LocString("cards", MissingPromptKey);
        __result.Add("ClassName", __instance.GetType().Name);
        __instance.DynamicVars.AddTo(__result);
        return false;
    }
}
