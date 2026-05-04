namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>
/// Card-library filter row: neutral (ignore), include (OR among checks), require-and (O: must match all O rows),
/// exclude (✕: hide matches; evaluated before OR/O).
/// </summary>
public enum CardLibraryFilterTriState : sbyte
{
    Exclude = -1,
    Neutral = 0,
    Include = 1,
    RequireAnd = 2
}
