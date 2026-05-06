using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards;

/// <summary>
/// Central lists for <see cref="YgoCardArchetype"/>.
/// Blue-Eyes and Dark Magician use two lists each: <b>strict</b> (in-archetype for rules / keywords via
/// <see cref="GetStrictArchetypeTypes"/>) and <b>related</b> (broader support for <see cref="YgoRelatedCardsComposer"/> via
/// <see cref="GetTypes"/>). Other flags use a single list for both.
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
        //typeof(Blue_Eyes_Toon_Dragon),
        typeof(Paladin_of_White_Dragon),
        typeof(Dragon_Master_Knight),
        typeof(Burst_Stream_of_Destruction),
        typeof(White_Dragon_Ritual)
    ];

    private static readonly Type[] s_darkMagician =
    [
        typeof(Dark_Magician),
        typeof(Dark_Magician_Girl),
        typeof(Skilled_Dark_Magician),
        typeof(Dark_Magician_of_Chaos),
        //typeof(Toon_Dark_Magician_Girl),
        typeof(Magician_of_Black_Chaos),
        typeof(Dark_Paladin),
        typeof(Dark_Flare_Knight),
        typeof(Dark_Sage),
        typeof(Dark_Magic_Attack),
        typeof(Black_Magic_Ritual),
    ];

    private static readonly Type[] s_blueEyesRelated =
    [
        typeof(Blue_Eyes_White_Dragon),
        typeof(Blue_Eyes_Ultimate_Dragon),
        //typeof(Blue_Eyes_Toon_Dragon),
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

    private static readonly Type[] s_darkMagicianRelated =
    [
        typeof(Dark_Magician),
        typeof(Dark_Magician_Girl),
        typeof(Skilled_Dark_Magician),
        typeof(Dark_Magician_of_Chaos),
        //typeof(Toon_Dark_Magician_Girl),
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
        typeof(Winged_Minion),
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

    private static readonly Type[] s_spellCounter =
    [
        typeof(Anti_Spell),
        typeof(Apprentice_Magician),
        typeof(Breaker_the_Magical_Warrior),
        typeof(Hannibal_Necromancer),
        typeof(Legendary_Flame_Lord),
        typeof(Magical_Marionette),
        typeof(Magical_Plant_Mandragola),
        //typeof(Pharaoh_s_Treasure),
        //typeof(Pitch_Black_Power_Stone),
        typeof(Royal_Magical_Library),
        typeof(Skilled_Dark_Magician),
        typeof(Skilled_White_Magician),
        typeof(Incandescent_Ordeal),
    ];

    private static readonly Type[] s_redEyesBlackDragon =
    [
        typeof(Red_Eyes_Black_Dragon),
        //typeof(Red_Eyes_Black_Metal_Dragon),
        typeof(Meteor_Dragon),
        typeof(Summoned_Skull),
        typeof(Meteor_Black_Dragon),
        typeof(Black_Skull_Dragon),
        //typeof(Metalmorph),
    ];

    private static readonly Type[] s_harpieLady =
    [
        typeof(Harpie_Lady),
        //typeof(Cyber_Harpie_Lady),
        //typeof(Harpie_Lady_Sisters),
        //typeof(Harpie_S_Pet_Dragon),
        //typeof(Birdface),
        //typeof(Elegant_Egotist),
        //typeof(Harpie_S_Feather_Duster),
    ];

    private static readonly Type[] s_coinflip =
    [
        typeof(Fairy_Box),
        //typeof(Second_Coin_Toss),
        typeof(Jirai_Gumo),
        typeof(Copycat),
        typeof(Heads),
        typeof(Tails),
    ];

    private static readonly Type[] s_diceroll =
    [
        typeof(Graceful_Dice),
        typeof(Skull_Dice),
        //typeof(Dice_Re_Roll),
        typeof(Dice_Jar),
        typeof(Dice_Armadillo),
        typeof(Blind_Destruction),
    ];

    private static readonly Type[] s_blight =
    [
        typeof(Alligator_S_Sword_Dragon),
        typeof(Amphibious_Bugroth_MK_3),
        typeof(Black_Tyranno),
        typeof(Drillago),
        typeof(Gear_Golem_the_Moving_Fortress),
        typeof(Jinzo_7),
        typeof(Lady_Assailant_of_Flames),
        typeof(Leghul),
        typeof(Levia_Dragon_Daedalus),
        typeof(Mucus_Yolk),
        typeof(Mystic_Lamp),
        typeof(Nightmare_Horse),
        typeof(Ocean_Dragon_Lord_Neo_Daedalus),
        typeof(Ooguchi),
        typeof(Piranha_Army),
        typeof(Queen_s_Double),
        typeof(Rainbow_Flower),
        typeof(Reaper_on_the_Nightmare),
        typeof(Rocket_Jumper),
        typeof(Secret_Pass_to_the_Treasures),
        typeof(Servant_of_Catabolism),
        typeof(Spear_Dragon),
        //typeof(Toon_Dark_Magician_Girl),
        //typeof(Toon_Mermaid),
        //typeof(Toon_Summoned_Skull),
        typeof(Yomi_Ship),
    ];

    private static readonly Type[] s_splinter =
    [
        typeof(Airknight_Parshath),
        typeof(Big_Bang_Shot),
        typeof(Dark_Driceratops),
        typeof(Dragon_Nails),
        typeof(Enraged_Battle_Ox),
        typeof(Exarion_Universe),
        typeof(Gravekeeper_s_Spear_Soldier),
        typeof(Insect_Armor_with_Laser_Cannon),
        typeof(Mad_Sword_Beast),
        typeof(Mefist_the_Infernal_General),
        typeof(Sword_of_Dragon_S_Soul),
    ];

    private static readonly Type[] s_takeDamage =
    [
        typeof(Archfiend_s_Oath),
        typeof(Extra_Foolish_Burial),
        typeof(Fairy_Box),
        typeof(Jirai_Gumo),
        typeof(Mausoleum_of_the_Emperor),
    ];

    private static readonly Type[] s_doomed =
    [
        typeof(Cyber_Stein),
        typeof(Gale_Dogra),
        typeof(Magical_Scientist),
        typeof(Monster_Eye),
        typeof(The_Winged_Dragon_of_Ra),
    ];

    private static readonly Type[] s_growthType =
    [
        typeof(D_D_Crazy_Beast),
        typeof(Obelisk_the_Tormentor),
        typeof(Slifer_the_Sky_Dragon),
        typeof(Sword_Hunter),
        typeof(Terrorking_Archfiend),
        typeof(The_Last_Warrior_from_Another_Planet),
        typeof(The_Winged_Dragon_of_Ra),
    ];

    private static readonly Type[] s_gravekeeper =
    [
        typeof(Necrovalley),
        typeof(Gravekeeper_s_Assailant),
        typeof(Gravekeeper_s_Cannonholder),
        typeof(Gravekeeper_s_Chief),
        typeof(Gravekeeper_s_Curse),
        typeof(Gravekeeper_s_Guard),
        typeof(Gravekeeper_s_Spear_Soldier),
        typeof(Gravekeeper_s_Spy),
    ];

    private static readonly Type[] s_umi =
    [
        typeof(Umi),
        typeof(A_Legendary_Ocean),
        typeof(Power_Of_Kaishin),
        typeof(The_Legendary_Fisherman),
        typeof(Levia_Dragon_Daedalus),
        typeof(Ocean_Dragon_Lord_Neo_Daedalus),
    ];

    private static readonly Type[] s_summonHeal =
    [
        typeof(Dancing_Fairy),
        typeof(Gilasaurus),
        typeof(Granadora),
    ];

    private static readonly Type[] s_selfHeal =
    [
        typeof(Absorbing_Kid_from_the_Sky),
        typeof(Cestus_of_Dagla),
        typeof(Cure_Mermaid),
        typeof(Dian_Keto_the_Cure_Master),
        typeof(Draining_Shield),
        typeof(Emergency_Provisions),
        typeof(Enchanted_Javelin),
        typeof(Fire_Princess),
        typeof(Poison_of_the_Old_Man),
        typeof(Rain_of_Mercy),
        typeof(Skull_Mark_Ladybug),
        typeof(Solemn_Wishes),
        typeof(Token_Thanksgiving),
        //typeof(Zolga),
    ];

    private static readonly Type[] s_quickBlock =
    [
        typeof(Breath_of_Light),
        typeof(Cold_Wave),
        typeof(Dancing_Fairy),
        typeof(Dark_Piercing_Light),
        typeof(Kuriboh),
    ];

    private static readonly Type[] s_slowBlock =
    [
        typeof(Anti_Spell),
        typeof(Deal_of_Phantom),
        typeof(Spell_Shield_Type_8),
    ];

    private static readonly Type[] s_genericAllMonstersTempStatBoost =
    [
        typeof(Castle_Walls),
        typeof(Graceful_Dice),
        typeof(Pyramid_Energy),
        typeof(Reinforcements),
    ];

    private static readonly Type[] s_genericAllMonstersContinuousStatBoost =
    [
        typeof(Banner_of_Courage),
        typeof(The_A_Forces),
        typeof(Yellow_Luster_Shield),
    ];

    private static readonly Type[] s_genericSingleMonsterTempStatBoost =
    [
        typeof(Riryoku),
        typeof(Rush_Recklessly),
        typeof(The_Reliable_Guardian),
    ];

    private static readonly Type[] s_energy =
    [
        typeof(Cost_Down),
        typeof(De_Spell),
        typeof(Fake_Trap),
        typeof(Mask_of_Brutality),
        typeof(Mask_of_the_Burdened),
        typeof(Mask_of_Weakness),
        typeof(Mausoleum_of_the_Emperor),
        typeof(Narrow_Pass),
    ];

    private static readonly object s_genericGate = new();
    private static Type[]? s_genericDoubleSummoner;

    private static readonly object s_divineBeastGate = new();
    private static Type[]? s_divineBeast;

    private static readonly Dictionary<YgoCardArchetype, Type[]> s_byArchetype = new()
    {
        [YgoCardArchetype.ZombieBoost] = s_zombieBoost,
        [YgoCardArchetype.BlueEyesWhiteDragon] = s_blueEyesRelated,
        [YgoCardArchetype.DarkMagician] = s_darkMagicianRelated,
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
        [YgoCardArchetype.SpellCounter] = s_spellCounter,
        [YgoCardArchetype.RedEyesBlackDragon] = s_redEyesBlackDragon,
        [YgoCardArchetype.HarpieLady] = s_harpieLady,
        [YgoCardArchetype.Coinflip] = s_coinflip,
        [YgoCardArchetype.Diceroll] = s_diceroll,
        [YgoCardArchetype.Blight] = s_blight,
        [YgoCardArchetype.Splinter] = s_splinter,
        [YgoCardArchetype.TakeDamage] = s_takeDamage,
        [YgoCardArchetype.Doomed] = s_doomed,
        [YgoCardArchetype.GrowthType] = s_growthType,
        [YgoCardArchetype.Gravekeeper] = s_gravekeeper,
        [YgoCardArchetype.Umi] = s_umi,
        [YgoCardArchetype.SummonHeal] = s_summonHeal,
        [YgoCardArchetype.SelfHeal] = s_selfHeal,
        [YgoCardArchetype.QuickBlock] = s_quickBlock,
        [YgoCardArchetype.SlowBlock] = s_slowBlock,
        [YgoCardArchetype.GenericAllMonstersTempStatBoost] = s_genericAllMonstersTempStatBoost,
        [YgoCardArchetype.GenericAllMonstersContinuousStatBoost] = s_genericAllMonstersContinuousStatBoost,
        [YgoCardArchetype.GenericSingleMonsterTempStatBoost] = s_genericSingleMonsterTempStatBoost,
        [YgoCardArchetype.Energy] = s_energy,
    };

    /// <summary>
    /// Related pool for <paramref name="archetype"/> (pack weighting, <see cref="YgoRelatedCardsComposer"/>,
    /// <see cref="GetImplicitArchetypes"/>). For <see cref="YgoCardArchetype.BlueEyesWhiteDragon"/> and
    /// <see cref="YgoCardArchetype.DarkMagician"/> this is the broader support list; use <see cref="GetStrictArchetypeTypes"/>
    /// for in-archetype card logic and monster keywords.
    /// </summary>
    public static IReadOnlyList<Type> GetTypes(YgoCardArchetype archetype)
    {
        if (archetype == YgoCardArchetype.None)
            return Array.Empty<Type>();

        if (archetype == YgoCardArchetype.GenericDoubleSummoner)
            return EnsureGenericDoubleSummoner();

        if (archetype == YgoCardArchetype.DivineBeast)
            return EnsureDivineBeast();

        return s_byArchetype.TryGetValue(archetype, out Type[]? arr)
            ? arr
            : Array.Empty<Type>();
    }

    /// <summary>
    /// Strict "in this archetype" members (YGO naming): monsters and cards that count for archetype rules / keyword chips.
    /// For Blue-Eyes and Dark Magician only; other archetypes delegate to <see cref="GetTypes"/>.
    /// </summary>
    public static IReadOnlyList<Type> GetStrictArchetypeTypes(YgoCardArchetype archetype) =>
        archetype switch
        {
            YgoCardArchetype.BlueEyesWhiteDragon => s_blueEyes,
            YgoCardArchetype.DarkMagician => s_darkMagician,
            _ => GetTypes(archetype),
        };

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

        if (Contains(EnsureDivineBeast(), cardType))
            f |= YgoCardArchetype.DivineBeast;

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
            return s_genericDoubleSummoner;
        }
    }

    private static Type[] EnsureDivineBeast()
    {
        lock (s_divineBeastGate)
        {
            if (s_divineBeast != null)
                return s_divineBeast;

            var list = new List<Type>();
            foreach (CardModel c in YgoPackCardCatalog.GetAllYgoTemplates())
            {
                if (c is not YgoDuelistCard y)
                    continue;
                if ((y.PackTags & YgoCardPackTags.God) == 0)
                    continue;
                list.Add(c.GetType());
            }

            list.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            s_divineBeast = list.ToArray();
            return s_divineBeast;
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
