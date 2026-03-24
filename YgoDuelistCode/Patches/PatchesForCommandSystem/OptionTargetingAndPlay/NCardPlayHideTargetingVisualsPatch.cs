using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// Vanilla <c>NCardPlay.HideTargetingVisuals</c> uses <c>NCombatRoom.Instance.CreatureNodes</c> with no null check.
/// After <c>await</c> in <see cref="NMouseCardPlay.StartAsync"/>, cleanup paths (e.g. cancel / unplayable) can run when the combat room is already gone, which throws and surfaces as <c>StartAsync</c> in logs.
/// </summary>
[HarmonyPatch(typeof(NCardPlay), "HideTargetingVisuals")]
public static class NCardPlayHideTargetingVisualsPatch
{
    static bool Prefix(NCardPlay __instance)
    {
        var room = NCombatRoom.Instance;
        if (room != null)
        {
            foreach (var creatureNode in room.CreatureNodes)
            {
                creatureNode.HideMultiselectReticle();
            }
        }

        NCard? cardNode = __instance.Holder?.CardNode;
        cardNode?.SetPreviewTarget(null);
        var model = cardNode?.Model;
        cardNode?.UpdateVisuals((model?.Pile?.Type).GetValueOrDefault(), CardPreviewMode.Normal);
        return false;
    }
}
