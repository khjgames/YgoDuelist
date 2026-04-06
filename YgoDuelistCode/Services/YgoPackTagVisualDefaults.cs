using Godot;
using YgoDuelist.YgoDuelistCode.Cards;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Default RGB multiply tints for sealed pack tag layers (single-bit <see cref="YgoCardPackTags"/> each).</summary>
public static class YgoPackTagVisualDefaults
{
    public static Color GetTintColor(YgoCardPackTags singleBit)
    {
        if (YgoPackTagBits.PopCount(singleBit) != 1)
            throw new ArgumentException("Expected exactly one pack tag bit.", nameof(singleBit));

        return singleBit switch
        {
            YgoCardPackTags.Earth => new Color("8B4513"),
            YgoCardPackTags.Water => new Color("1E90FF"),
            YgoCardPackTags.Wind => new Color("B0E0E6"),
            YgoCardPackTags.Fire => new Color("DC143C"),
            YgoCardPackTags.Dark => new Color("2F1847"),
            YgoCardPackTags.Light => new Color("FFFACD"),
            YgoCardPackTags.Fusion => new Color("9932CC"),
            YgoCardPackTags.Ritual => new Color("4169E1"),
            YgoCardPackTags.Ocean => new Color("006994"),
            YgoCardPackTags.Insect => new Color("9ACD32"),
            YgoCardPackTags.Machine => new Color("708090"),
            YgoCardPackTags.Dragon => new Color("B22222"),
            YgoCardPackTags.Zombie => new Color("556B2F"),
            YgoCardPackTags.Fiend => new Color("8B008B"),
            YgoCardPackTags.Spellcaster => new Color("6A5ACD"),
            YgoCardPackTags.Warrior => new Color("CD853F"),
            YgoCardPackTags.Heal => new Color("98FB98"),
            YgoCardPackTags.Draw => new Color("FFD700"),
            YgoCardPackTags.Chance => new Color("FF8C00"),
            YgoCardPackTags.Burn => new Color("FF4500"),
            YgoCardPackTags.Normal => new Color("FFFF00"),
            YgoCardPackTags.Spell => new Color("228B22"),
            YgoCardPackTags.Trap => new Color("FF69B4"),
            YgoCardPackTags.Banish => new Color("E0FFFF"),
            YgoCardPackTags.WinCon => new Color("FFD700"),
            YgoCardPackTags.God => new Color("F5DEB3"),
            YgoCardPackTags.Bundled => new Color("A0522D"),
            YgoCardPackTags.Starter => new Color("D3D3D3"),
            _ => Colors.White
        };
    }
}
