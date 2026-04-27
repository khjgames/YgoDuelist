using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class DuelMonsterScalePatch
{
    public static void Postfix(Creature creature)
    {
        if (creature?.Monster is not DuelMonsterModel)
            return;

        DuelMonsterVisualLayout.ApplyNewlyAddedDuelMonster(creature);
    }
}
