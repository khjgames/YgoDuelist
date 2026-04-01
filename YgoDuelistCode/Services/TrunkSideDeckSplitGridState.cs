using MegaCrit.Sts2.Core.Nodes.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// While the trunk/side split editor is open, two <see cref="NCardGrid"/> instances are active; selection highlight must target the grid that owns the card.
/// </summary>
public static class TrunkSideDeckSplitGridState
{
    public static bool Active { get; private set; }

    public static NCardGrid? LeftGrid { get; private set; }

    public static NCardGrid? RightGrid { get; private set; }

    public static void Activate(NCardGrid left, NCardGrid right)
    {
        LeftGrid = left;
        RightGrid = right;
        Active = true;
    }

    public static void Clear()
    {
        Active = false;
        LeftGrid = null;
        RightGrid = null;
    }
}
