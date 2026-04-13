using System;
using System.Collections.Generic;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards;

/// <summary>
/// Central lists for <see cref="YgoCardArchetype"/> used by <see cref="YgoRelatedCardsComposer"/>.
/// Attribute/race boost sets align with <see cref="YgoStarterCardCatalog"/> flat attribute fields, terrain fields, and flat race equips.
/// </summary>
public static class YgoCardArchetypeRegistry
{
    private static readonly Type[] s_zombieBoost =
    [
        typeof(Pumpking_the_King_of_Ghosts),
        typeof(Castle_of_Dark_Illusions),
        typeof(Wasteland),
        typeof(Violet_Crystal),
    ];

    private static readonly Type[] s_blueEyes =
    [
        typeof(Blue_Eyes_White_Dragon),
        typeof(Blue_Eyes_Ultimate_Dragon),
        typeof(Blue_Eyes_Toon_Dragon),
        typeof(Paladin_of_White_Dragon),
        typeof(Dragon_Master_Knight),
        typeof(Burst_Stream_of_Destruction),
        typeof(White_Dragon_Ritual),
        typeof(Double_Summon),
        typeof(Mausoleum_of_the_Emperor),
        typeof(Cost_Down),
        typeof(Totem_Dragon),
        typeof(Samsara_Dragon),
        typeof(Keeper_of_the_Shrine),
        typeof(Kaiser_Sea_Horse),
        typeof(Light_Effigy),
    ];

    private static readonly Type[] s_darkMagician =
    [
        typeof(Dark_Magician),
        typeof(Dark_Magician_Girl),
        typeof(Skilled_Dark_Magician),
        typeof(Dark_Magician_of_Chaos),
        typeof(Toon_Dark_Magician_Girl),
        typeof(Magician_of_Black_Chaos),
        typeof(Dark_Paladin),
        typeof(Dark_Flare_Knight),
        typeof(Dark_Sage),
        typeof(Dark_Magic_Attack),
        typeof(Double_Summon),
        typeof(Mausoleum_of_the_Emperor),
        typeof(Cost_Down),
        typeof(Dark_Effigy),
        typeof(Double_Coston),
        typeof(Black_Magic_Ritual),
    ];

    private static readonly Type[] s_earthBoost =
    [
        typeof(Milus_Radiant),
        typeof(Gaia_Power),
        typeof(Forest),
    ];

    private static readonly Type[] s_waterBoost =
    [
        typeof(Star_Boy),
        typeof(Umiiruka),
        typeof(Umi),
        typeof(A_Legendary_Ocean),
    ];

    private static readonly Type[] s_windBoost =
    [
        typeof(Bladefly),
        typeof(Rising_Air_Current),
        typeof(Forest),
    ];

    private static readonly Type[] s_fireBoost =
    [
        typeof(Little_Chimera),
        typeof(Molten_Destruction),
        typeof(Forest),
    ];

    private static readonly Type[] s_darkBoost =
    [
        typeof(Witchs_Apprentice),
        typeof(Mystic_Plasma_Zone),
        typeof(Yami),
    ];

    private static readonly Type[] s_lightBoost =
    [
        typeof(Hoshiningen),
        typeof(Luminous_Spark),
        typeof(Yami),
    ];

    private static readonly Type[] s_aquaBoost =
    [
        typeof(Power_Of_Kaishin),
        typeof(Umi),
    ];

    private static readonly Type[] s_beastBoost =
    [
        typeof(Beast_Fangs),
        typeof(Forest),
    ];

    private static readonly Type[] s_beastWarriorBoost =
    [
        typeof(Mystical_Moon),
        typeof(Sogen),
        typeof(Forest),
    ];

    private static readonly Type[] s_dinosaurBoost =
    [
        typeof(Raise_Body_Heat),
        typeof(Wasteland),
    ];

    private static readonly Type[] s_dragonBoost =
    [
        typeof(Dragon_Treasure),
        typeof(Mountain),
    ];

    private static readonly Type[] s_fairyBoost =
    [
        typeof(Silver_Bow_And_Arrow),
    ];

    private static readonly Type[] s_fiendBoost =
    [
        typeof(Dark_Energy),
        typeof(Yami),
    ];

    private static readonly Type[] s_insectBoost =
    [
        typeof(Laser_Cannon_Armor),
        typeof(Forest),
    ];

    private static readonly Type[] s_machineBoost =
    [
        typeof(Machine_Conversion_Factory),
    ];

    private static readonly Type[] s_plantBoost =
    [
        typeof(Vile_Germs),
        typeof(Forest),
    ];

    private static readonly Type[] s_spellcasterBoost =
    [
        typeof(Book_Of_Secret_Arts),
        typeof(Yami),
    ];

    private static readonly Type[] s_thunderBoost =
    [
        typeof(Electro_Whip),
        typeof(Mountain),
        typeof(Umi),
    ];

    private static readonly Type[] s_warriorBoost =
    [
        typeof(Legendary_Sword),
        typeof(Sogen),
    ];

    private static readonly Type[] s_wingedBeastBoost =
    [
        typeof(Follow_Wind),
        typeof(Mountain),
    ];

    private static readonly Type[] s_rockBoost =
    [
        typeof(Wasteland),
        typeof(Giant_Soldier_of_Stone),
    ];

    private static readonly object s_genericGate = new();
    private static Type[]? s_genericDoubleSummoner;

    private static readonly Dictionary<YgoCardArchetype, Type[]> s_byArchetype = new()
    {
        [YgoCardArchetype.ZombieBoost] = s_zombieBoost,
        [YgoCardArchetype.BlueEyesWhiteDragon] = s_blueEyes,
        [YgoCardArchetype.DarkMagician] = s_darkMagician,
        [YgoCardArchetype.EarthBoost] = s_earthBoost,
        [YgoCardArchetype.WaterBoost] = s_waterBoost,
        [YgoCardArchetype.WindBoost] = s_windBoost,
        [YgoCardArchetype.FireBoost] = s_fireBoost,
        [YgoCardArchetype.DarkBoost] = s_darkBoost,
        [YgoCardArchetype.LightBoost] = s_lightBoost,
        [YgoCardArchetype.AquaBoost] = s_aquaBoost,
        [YgoCardArchetype.BeastBoost] = s_beastBoost,
        [YgoCardArchetype.BeastWarriorBoost] = s_beastWarriorBoost,
        [YgoCardArchetype.DinosaurBoost] = s_dinosaurBoost,
        [YgoCardArchetype.DragonBoost] = s_dragonBoost,
        [YgoCardArchetype.FairyBoost] = s_fairyBoost,
        [YgoCardArchetype.FiendBoost] = s_fiendBoost,
        [YgoCardArchetype.InsectBoost] = s_insectBoost,
        [YgoCardArchetype.MachineBoost] = s_machineBoost,
        [YgoCardArchetype.PlantBoost] = s_plantBoost,
        [YgoCardArchetype.SpellcasterBoost] = s_spellcasterBoost,
        [YgoCardArchetype.ThunderBoost] = s_thunderBoost,
        [YgoCardArchetype.WarriorBoost] = s_warriorBoost,
        [YgoCardArchetype.WingedBeastBoost] = s_wingedBeastBoost,
        [YgoCardArchetype.RockBoost] = s_rockBoost,
    };

    /// <summary>All card types in <paramref name="archetype"/> (for <see cref="YgoRelatedCardsComposer"/>).</summary>
    public static IReadOnlyList<Type> GetTypes(YgoCardArchetype archetype)
    {
        if (archetype == YgoCardArchetype.None)
            return Array.Empty<Type>();

        if (archetype == YgoCardArchetype.GenericDoubleSummoner)
            return EnsureGenericDoubleSummoner();

        return s_byArchetype.TryGetValue(archetype, out Type[]? arr)
            ? arr
            : Array.Empty<Type>();
    }

    /// <summary>
    /// Pack-related archetypes inferred from card type (in addition to <see cref="YgoDuelistCard.CardArchetypes"/>).
    /// </summary>
    public static YgoCardArchetype GetImplicitArchetypes(Type cardType)
    {
        YgoCardArchetype f = YgoCardArchetype.None;
        foreach (KeyValuePair<YgoCardArchetype, Type[]> kv in s_byArchetype)
        {
            if (Contains(kv.Value, cardType))
                f |= kv.Key;
        }

        foreach (Type t in DoubleTributeMaterialCatalog.DoubleTributeMaterialCardTypes)
        {
            if (t == cardType)
            {
                f |= YgoCardArchetype.GenericDoubleSummoner;
                break;
            }
        }

        return f;
    }

    private static Type[] EnsureGenericDoubleSummoner()
    {
        lock (s_genericGate)
        {
            if (s_genericDoubleSummoner != null)
                return s_genericDoubleSummoner;

            IReadOnlyList<Type> src = DoubleTributeMaterialCatalog.DoubleTributeMaterialCardTypes;
            var arr = new Type[src.Count];
            for (int i = 0; i < src.Count; i++)
                arr[i] = src[i];
            s_genericDoubleSummoner = arr;
            return arr;
        }
    }

    private static bool Contains(Type[] list, Type t)
    {
        foreach (Type x in list)
        {
            if (x == t)
                return true;
        }

        return false;
    }
}
