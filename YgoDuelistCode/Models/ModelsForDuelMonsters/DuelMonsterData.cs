namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Data for summoning a duel monster from a card. Stats are applied from the card at summon time.
/// </summary>
public sealed class DuelMonsterData
{
    /// <summary>Level (star count) 1-9+ used for summon HP table.</summary>
    public int Level { get; }

    /// <summary>ATK value from card (for display / future use).</summary>
    public int Atk { get; }

    /// <summary>DEF value from card (for display / future use).</summary>
    public int Def { get; }

    /// <summary>Portrait image path for the summon visual (card art). Used when custom duel monster visuals support it.</summary>
    public string? PortraitPath { get; }

    /// <summary>Localization table for the summon name (e.g. \"cards\").</summary>
    public string LocTable { get; }

    /// <summary>Localization key for the summon name (e.g. \"MUKA_MUKA.title\").</summary>
    public string LocKey { get; }

    public DuelMonsterData(int level, int atk, int def, string locTable, string locKey, string? portraitPath = null)
    {
        Level = level;
        Atk = atk;
        Def = def;
        LocTable = locTable;
        LocKey = locKey;
        PortraitPath = portraitPath;
    }
}
