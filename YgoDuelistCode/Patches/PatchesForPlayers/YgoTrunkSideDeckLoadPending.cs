using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Patches;

internal sealed class YgoTrunkSideDeckLoadPending
{
    public List<SerializableCard> Trunk { get; } = new();
    public List<SerializableCard> Side { get; } = new();
}
