using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Equip;

/// <summary>+3 ATK/DEF in combat scale (YGO 300); curated flat equips from cards_database.json.</summary>
public abstract class FlatRaceEquipSpell : BaseEquipSpellCard
{
    private readonly DuelMonsterRace _requiredRace;
    private readonly int _bonusAtk;
    private readonly int _bonusDef;

    protected FlatRaceEquipSpell(CardRarity rarity, DuelMonsterRace requiredRace, int bonusAtk, int bonusDef)
        : base(1, rarity, TargetType.Self)
    {
        _requiredRace = requiredRace;
        _bonusAtk = bonusAtk;
        _bonusDef = bonusDef;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public sealed override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == _requiredRace;

    public sealed override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(_bonusAtk, _bonusDef);
}

public sealed class Beast_Fangs : FlatRaceEquipSpell
{
    public Beast_Fangs() : base(CardRarity.Common, DuelMonsterRace.Beast, 3, 3) { }
}

public sealed class Book_Of_Secret_Arts : FlatRaceEquipSpell
{
    public Book_Of_Secret_Arts() : base(CardRarity.Common, DuelMonsterRace.Spellcaster, 3, 3) { }
}

public sealed class Dark_Energy : FlatRaceEquipSpell
{
    public Dark_Energy() : base(CardRarity.Common, DuelMonsterRace.Fiend, 3, 3) { }
}

public sealed class Dragon_Treasure : FlatRaceEquipSpell
{
    public Dragon_Treasure() : base(CardRarity.Common, DuelMonsterRace.Dragon, 3, 3) { }
}

public sealed class Electro_Whip : FlatRaceEquipSpell
{
    public Electro_Whip() : base(CardRarity.Common, DuelMonsterRace.Thunder, 3, 3) { }
}

public sealed class Follow_Wind : FlatRaceEquipSpell
{
    public Follow_Wind() : base(CardRarity.Common, DuelMonsterRace.WingedBeast, 3, 3) { }
}

public sealed class Laser_Cannon_Armor : FlatRaceEquipSpell
{
    public Laser_Cannon_Armor() : base(CardRarity.Common, DuelMonsterRace.Insect, 3, 3) { }
}

public sealed class Legendary_Sword : FlatRaceEquipSpell
{
    public Legendary_Sword() : base(CardRarity.Common, DuelMonsterRace.Warrior, 3, 3) { }
}

public sealed class Machine_Conversion_Factory : FlatRaceEquipSpell
{
    public Machine_Conversion_Factory() : base(CardRarity.Common, DuelMonsterRace.Machine, 3, 3) { }
}

public sealed class Mystical_Moon : FlatRaceEquipSpell
{
    public Mystical_Moon() : base(CardRarity.Common, DuelMonsterRace.BeastWarrior, 3, 3) { }
}

public sealed class Power_Of_Kaishin : FlatRaceEquipSpell
{
    public Power_Of_Kaishin() : base(CardRarity.Common, DuelMonsterRace.Aqua, 3, 3) { }
}

public sealed class Raise_Body_Heat : FlatRaceEquipSpell
{
    public Raise_Body_Heat() : base(CardRarity.Common, DuelMonsterRace.Dinosaur, 3, 3) { }
}

public sealed class Silver_Bow_And_Arrow : FlatRaceEquipSpell
{
    public Silver_Bow_And_Arrow() : base(CardRarity.Common, DuelMonsterRace.Fairy, 3, 3) { }
}

public sealed class Vile_Germs : FlatRaceEquipSpell
{
    public Vile_Germs() : base(CardRarity.Common, DuelMonsterRace.Plant, 3, 3) { }
}

public sealed class Violet_Crystal : FlatRaceEquipSpell
{
    public Violet_Crystal() : base(CardRarity.Common, DuelMonsterRace.Zombie, 3, 3) { }
}
