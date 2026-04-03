namespace YgoDuelist.YgoDuelistCode.Nodes.CardLibrary;

/// <summary>Tri-state card-library filter row: neutral (ignore), include (OR), exclude (hide matches).</summary>
public enum CardLibraryFilterTriState : sbyte
{
    Exclude = -1,
    Neutral = 0,
    Include = 1
}
