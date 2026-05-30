using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

internal static class YgoTriStateRarityTickRegistry
{
    static readonly ConditionalWeakTable<NCardRarityTickbox, YgoTriStateRarityTickController> Table = new();

    internal static void Register(NCardRarityTickbox box, YgoTriStateRarityTickController controller) =>
        Table.Add(box, controller);

    internal static bool TryGet(NCardRarityTickbox box, out YgoTriStateRarityTickController? controller)
    {
        if (Table.TryGetValue(box, out YgoTriStateRarityTickController? c) && c is not null)
        {
            controller = c;
            return true;
        }

        controller = null;
        return false;
    }
}
