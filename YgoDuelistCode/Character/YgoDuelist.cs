using BaseLib.Abstracts;
using YgoDuelist.YgoDuelistCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using YgoDuelist.YgoDuelistCode.Cards.Basic;
using YgoDuelist.YgoDuelistCode.Cards.Monster;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Elemental;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Cards.Spell;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Character;

public class YgoDuelist : PlaceholderCharacterModel
{
    public const string CharacterId = "YgoDuelist";
    public const string energyColorName = "regent";

    public static readonly Color Color = new("ffffff");

    public override Color NameColor => Color;
    public override bool ShouldAlwaysShowStarCounter => true;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 80;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<Strike_YgoDuelist>(),
        ModelDb.Card<Strike_YgoDuelist>(),
        ModelDb.Card<Strike_YgoDuelist>(),
        ModelDb.Card<Defend_YgoDuelist>(),
        ModelDb.Card<Defend_YgoDuelist>(),
        ModelDb.Card<Defend_YgoDuelist>()
    ];

    /*
        ModelDb.Card<Amazoness_Blowpiper>(),
        ModelDb.Card<Amazoness_Swords_Woman>(),
        ModelDb.Card<Amazoness_Tiger>(),
        ModelDb.Card<Anti_Aircraft_Flower>(),
        ModelDb.Card<Anti_Spell>(),
        ModelDb.Card<Appropriate>(),
        ModelDb.Card<Arcane_Archer_of_the_Forest>(),
        ModelDb.Card<Archfiend_s_Oath>(),
        ModelDb.Card<Arsenal_Robber>(),
        ModelDb.Card<Blast_Juggler>(),
        ModelDb.Card<Blind_Destruction>(),
        ModelDb.Card<Bad_Reaction_to_Simochi>(),
        ModelDb.Card<Upstart_Goblin>(),
        ModelDb.Card<Upstart_Goblin>(),
        ModelDb.Card<Upstart_Goblin>(),
        ModelDb.Card<Upstart_Goblin>(),
        ModelDb.Card<Book_of_Moon>(),
        ModelDb.Card<Book_of_Taiyou>(),
        ModelDb.Card<Bottomless_Shifting_Sand>(),
        ModelDb.Card<Bottomless_Trap_Hole>(),
        ModelDb.Card<Bowganian>(),
        ModelDb.Card<Burning_Algae>(),
        ModelDb.Card<Burst_Stream_of_Destruction>(),
        ModelDb.Card<Cat_s_Ear_Tribe>(),
        ModelDb.Card<Cestus_of_Dagla>(),
        ModelDb.Card<Chaos_Command_Magician>(),
        ModelDb.Card<Chaos_End>(),
        ModelDb.Card<Compulsory_Evacuation_Device>(),
        ModelDb.Card<Cure_Mermaid>(),
        ModelDb.Card<Curse_of_Aging>(),
        ModelDb.Card<Curse_of_Anubis>(),
        ModelDb.Card<Curse_of_Darkness>(),
        ModelDb.Card<Cursed_Seal_of_the_Forbidden_Spell>(),
        ModelDb.Card<Cyber_Jar>(),
        ModelDb.Card<D_D_Crazy_Beast>(),
        ModelDb.Card<D_D_Warrior_Lady>(),
        ModelDb.Card<Dancing_Fairy>(),
        ModelDb.Card<Dark_Cat_with_White_Tail>(),
        ModelDb.Card<Dark_Jeroid>(),
        ModelDb.Card<Dark_Magic_Attack>(),
        ModelDb.Card<Dark_Mirror_Force>(),
        ModelDb.Card<Dark_Snake_Syndrome>(),
        ModelDb.Card<Dark_Spirit_of_the_Silent>(),
        ModelDb.Card<Dark_Zebra>(),
        ModelDb.Card<Darklord_Marie>(),
        ModelDb.Card<Deal_of_Phantom>(),
        ModelDb.Card<Des_Counterblow>(),
        ModelDb.Card<Des_Kangaroo>(),
        ModelDb.Card<Diffusion_Wave_Motion>(),
        ModelDb.Card<Draining_Shield>(),
        ModelDb.Card<Dust_Barrier>(),
        ModelDb.Card<Elephant_Statue_of_Blessing>(),
        ModelDb.Card<Elephant_Statue_of_Disaster>(),
        ModelDb.Card<Energy_Drain>(),
        ModelDb.Card<Fairy_Box>(),
        ModelDb.Card<Fairy_Guardian>(),
        ModelDb.Card<Spell_Shield_Type_8>(),
        ModelDb.Card<Spellbinding_Circle>(),
        ModelDb.Card<Spellbook_Organization>(),
        ModelDb.Card<Spirit_of_the_Breeze>(),
        ModelDb.Card<Stumbling>(),
        ModelDb.Card<Super_Rejuvenation>(),
        ModelDb.Card<Sword_Hunter>(),
        ModelDb.Card<Tailor_of_the_Fickle>(),
        ModelDb.Card<Tainted_Wisdom>(),
        ModelDb.Card<Talisman_of_Spell_Sealing>(),
        ModelDb.Card<Talisman_of_Trap_Sealing>(),
        ModelDb.Card<Terrorking_Archfiend>(),
        ModelDb.Card<The_Sanctuary_in_the_Sky>(),
        ModelDb.Card<The_Agent_of_Wisdom_Mercury>(),
        ModelDb.Card<The_Bistro_Butcher>(),
        ModelDb.Card<The_Hunter_with_7_Weapons>(),
        ModelDb.Card<The_Law_of_the_Normal>(),
        ModelDb.Card<The_Legendary_Fisherman>(),
        ModelDb.Card<Rush_Recklessly>(),
        ModelDb.Card<The_Reliable_Guardian>(),
        ModelDb.Card<Tornado_Wall>(),
        ModelDb.Card<Torpedo_Fish>(),
        ModelDb.Card<Twin_Headed_Wolf>(),
        ModelDb.Card<Zone_Eater>(),
        ModelDb.Card<Possessed_Dark_Soul>(),
        ModelDb.Card<Pitch_Dark_Dragon>(),
        ModelDb.Card<Lord_of_D>(),
        ModelDb.Card<Zombyra_the_Dark>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Pot_Of_Greed>(),
        ModelDb.Card<Foolish_Burial>(),

        ModelDb.Card<Monster_Reborn>(),
        ModelDb.Card<Mountain>(),
        ModelDb.Card<Mystical_Sheep_1>(),
        ModelDb.Card<Umi>(),

        ModelDb.Card<Thousand_Needles>(),
        ModelDb.Card<Muka_Muka>(),
        ModelDb.Card<Enraged_Muka_Muka>(),
        
        ModelDb.Card<Polymerization>(),
        ModelDb.Card<Polymerization>(),

        ModelDb.Card<Thunder_Dragon>(),
        ModelDb.Card<Thunder_Dragon>(),
        ModelDb.Card<Thunder_Dragon>(),
        ModelDb.Card<Twin_Headed_Thunder_Dragon>(),

        ModelDb.Card<Blue_Eyes_White_Dragon>(),
        ModelDb.Card<Blue_Eyes_White_Dragon>(),
        ModelDb.Card<Blue_Eyes_White_Dragon>(),
        ModelDb.Card<Blue_Eyes_Ultimate_Dragon>()
    */

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<GraveyardRelic>(),
        ModelDb.Relic<ShadowRealmRelic>(),
        ModelDb.Relic<ExtraDeckRelic>(),
        ModelDb.Relic<TrunkSideDeckRelic>(),
        ModelDb.Relic<SpellTrapZoneRelic>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<YgoDuelistCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<YgoDuelistRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<YgoDuelistPotionPool>();

    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets.
        These are just some of the simplest assets, given some placeholders to differentiate your character with.
        You don't have to, but you're suggested to rename these images. */
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();

}
