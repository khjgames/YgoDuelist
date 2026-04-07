using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="CardModel.FromSerializable"/> calls <see cref="CardModel.AfterDeserialized"/> before enchantments and the upgrade loop,
/// so adding permanent execute ATK there is overwritten when those steps adjust <see cref="CardModel.DynamicVars"/>.
/// Re-apply saved printed-stat bonuses after the full deserialize pipeline.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
public static class CardModelFromSerializableMonsterPermanentStatsPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __result)
    {
        if (__result is DeprecatedCard)
            return;
        if (__result is BaseMonsterCard bm)
            bm.ApplySavedExecuteAtkBonusToPrintedDamage();
        if (__result is The_Last_Warrior_from_Another_Planet last)
            last.ApplySavedSummonAbsorbDefBonusToPrintedDefense();
    }
}
