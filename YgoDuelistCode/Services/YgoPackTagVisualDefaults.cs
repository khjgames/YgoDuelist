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
            YgoCardPackTags.Earth => new Color("722A08"),
            YgoCardPackTags.Water => new Color("003DD8"),
            YgoCardPackTags.Wind => new Color("4EB314"),
            YgoCardPackTags.Fire => new Color("FF0000"),
            YgoCardPackTags.Dark => new Color("431E3F"),
            YgoCardPackTags.Light => new Color("E5E56B"),
            YgoCardPackTags.Fusion => new Color("4E238B"),
            YgoCardPackTags.Ritual => new Color("002AA6"),
            YgoCardPackTags.Ocean => new Color("002397"),
            YgoCardPackTags.Insect => new Color("043500"),
            YgoCardPackTags.Machine => new Color("393752"),
            YgoCardPackTags.Dragon => new Color("AB0900"),
            YgoCardPackTags.Zombie => new Color("002F49"),
            YgoCardPackTags.Fiend => new Color("380E3C"),
            YgoCardPackTags.Spellcaster => new Color("300EB6"),
            YgoCardPackTags.Warrior => new Color("4BA200"),
            YgoCardPackTags.Heal => new Color("FFFFFF"),
            YgoCardPackTags.Draw => new Color("0D3616"),
            YgoCardPackTags.Chance => new Color("53264C"),
            YgoCardPackTags.Burn => new Color("840014"),
            YgoCardPackTags.Normal => new Color("FFFF00"),
            YgoCardPackTags.Spell => new Color("048636"),
            YgoCardPackTags.Trap => new Color("FF1AE2"),
            YgoCardPackTags.Banish => new Color("000000"),
            YgoCardPackTags.WinCon => new Color("2E0005"),
            YgoCardPackTags.God => new Color("111111"),
            YgoCardPackTags.Bundled => new Color("A0522D"),
            YgoCardPackTags.Starter => new Color("D3D3D3"),
            _ => Colors.White
        };
    }

    /// <summary>
    /// For sealed-pack fallback loads from <c>card_portraits/</c> only: match <see cref="YgoDuelistCard.PortraitPath"/> (lowercase, hyphens → underscores, collapse <c>__</c>).
    /// Primary pack art lives under <c>Sealed_Cardpacks/Tag_Portraits/</c> with authored names (hyphens allowed).
    /// </summary>
    public static string NormalizeTagPortraitFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return fileName;
        string s = fileName.Trim().ToLowerInvariant().Replace('-', '_');
        while (s.Contains("__", StringComparison.Ordinal))
            s = s.Replace("__", "_", StringComparison.Ordinal);
        return s;
    }

    public static String[] GetTagPortraits(YgoCardPackTags singleBit)
    {
        if (YgoPackTagBits.PopCount(singleBit) != 1)
            throw new ArgumentException("Expected exactly one pack tag bit.", nameof(singleBit));

        return singleBit switch
        {
            YgoCardPackTags.Earth => ["labyrinth_wall.png", "giant_soldier_of_stone.png", "the_earth_-_hex-sealed_fusion.png"],
            YgoCardPackTags.Water => ["star_boy.png", "crab_turtle.png", "aqua_dragon.png"],
            YgoCardPackTags.Wind => ["windstorm_of_etaqua.png", "wind_effigy.png", "rising_air_current.png"],
            YgoCardPackTags.Fire => ["meteor_black_dragon.png", "flame_ruler.png", "molten_destruction.png"],
            YgoCardPackTags.Dark => ["dark_flare_knight.png", "caius_the_shadow_monarch.png", "the_dark_-_hex-sealed_fusion.png"],
            YgoCardPackTags.Light => ["hoshiningen.png", "light_effigy.png", "the_light_-_hex-sealed_fusion.png"],
            YgoCardPackTags.Fusion => ["king_of_the_swamp.png", "polymerization.png", "twin-headed_thunder_dragon.png"],
            YgoCardPackTags.Ritual => ["black_magic_ritual.png", "hungry_burger.png", "manju_of_the_ten_thousand_hands.png"],
            YgoCardPackTags.Ocean => ["kaiser_sea_horse.png", "umi.png", "fire_kraken.png"],
            YgoCardPackTags.Insect => ["insect_queen.png", "forest.png", "laser_cannon_armor.png"],
            YgoCardPackTags.Machine => ["limiter_removal.png", "xyz-dragon_cannon.png", "machine_conversion_factory.png"],
            YgoCardPackTags.Dragon => ["lord_of_d.png", "blue-eyes_ultimate_dragon.png", "burst_breath.png"],
            YgoCardPackTags.Zombie => ["fire_reaper.png", "reaper_on_the_nightmare.png", "wasteland.png"],
            YgoCardPackTags.Fiend => ["masked_beast_des_gardius.png", "dark_energy.png", "theban_nightmare.png"],
            YgoCardPackTags.Spellcaster => ["dark_magician.png", "book_of_secret_arts.png", "skilled_white_magician.png"],
            YgoCardPackTags.Warrior => ["reinforcement_of_the_army.png", "black_luster_soldier.png", "moon_envoy.png"],
            YgoCardPackTags.Heal => ["draining_shield.png", "cure_mermaid.png", "emergency_provisions.png"],
            YgoCardPackTags.Draw => ["pot_of_greed.png", "card_destruction.png", "graceful_charity.png"],
            YgoCardPackTags.Chance => ["jirai_gumo.png", "fairy_box.png", "graceful_dice.png"],
            YgoCardPackTags.Burn => ["burning_land.png", "sparks.png", "raigeki_break.png"],
            YgoCardPackTags.Normal => ["gigobyte.png", "the_law_of_the_normal.png", "haniwa.png"],
            YgoCardPackTags.Spell => ["monster_reborn.png", "double_summon.png", "axe_of_despair.png"],
            YgoCardPackTags.Trap => ["spellbinding_circle.png", "dark_mirror_force.png", "call_of_the_haunted.png"],
            YgoCardPackTags.Banish => ["dimension_fusion.png", "chaos_end.png", "banisher_of_the_light.png"],
            YgoCardPackTags.WinCon => ["exodia_the_forbidden_one.png", "final_countdown.png", "destiny_board.png"],
            YgoCardPackTags.God => ["obelisk_the_tormentor.png", "slifer_the_sky_dragon.png", "the_winged_dragon_of_ra.png"],
            _ => ["aaaaaa.png", "bbbbbb.png", "cccccc.png"]
        };
    }
}
