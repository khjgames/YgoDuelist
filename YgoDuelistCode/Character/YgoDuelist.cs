using BaseLib.Abstracts;
using YgoDuelist.YgoDuelistCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
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
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
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
        ModelDb.Card<Hamburger_Recipe>(),
        ModelDb.Card<Turtle_Oath>(),
        ModelDb.Card<Hungry_Burger>(),
        ModelDb.Card<Crab_Turtle>(),
        ModelDb.Card<Monster_Reborn>(),
        ModelDb.Card<Molten_Destruction>(),
        ModelDb.Card<Mountain>(),
        ModelDb.Card<Umi>(),
        ModelDb.Card<Beast_Fangs>(),
        ModelDb.Card<Dragon_Treasure>(),
        ModelDb.Card<Electro_Whip>(),
        ModelDb.Card<Thousand_Needles>(),
        ModelDb.Card<Muka_Muka>(),
        ModelDb.Card<Enraged_Muka_Muka>(),
        ModelDb.Card<Witchs_Apprentice>(),
        ModelDb.Card<Milus_Radiant>(),
        ModelDb.Card<Bladefly>(),
        ModelDb.Card<Hoshiningen>(),
        ModelDb.Card<Little_Chimera>(),
        ModelDb.Card<Star_Boy>(),
        
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
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<GraveyardRelic>(),
        ModelDb.Relic<ExtraDeckRelic>(),
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