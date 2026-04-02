using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YgoDuelist.YgoDuelistCode.Patches;

internal sealed class YgoTrunkSideDeckLoadPending
{
    public List<SerializableCard> Extra { get; } = new();
    public List<SerializableCard> Trunk { get; } = new();
    public List<SerializableCard> Side { get; } = new();

    public int LoadedMinimumDeckSize { get; set; }

    public int LoadedOwedRareCardVouchers { get; set; }

    public string? LoadedPackTagBalance { get; set; }
}
