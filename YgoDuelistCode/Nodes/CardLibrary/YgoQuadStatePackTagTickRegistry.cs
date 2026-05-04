using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

internal static class YgoQuadStatePackTagTickRegistry
{
    static readonly ConditionalWeakTable<NCardRarityTickbox, YgoQuadStatePackTagTickController> Table = new();

    internal static void Register(NCardRarityTickbox box, YgoQuadStatePackTagTickController controller) =>
        Table.Add(box, controller);

    internal static bool TryGet(NCardRarityTickbox box, out YgoQuadStatePackTagTickController? controller)
    {
        if (Table.TryGetValue(box, out YgoQuadStatePackTagTickController c))
        {
            controller = c;
            return true;
        }

        controller = null;
        return false;
    }
}
