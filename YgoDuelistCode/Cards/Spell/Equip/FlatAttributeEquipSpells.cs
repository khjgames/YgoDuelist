using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Equip;

/// <summary>Attribute-gated equips: +Mgc ATK with optional −Mgc2 DEF (÷100 TCG scale).</summary>
public abstract class FlatAttributeEquipSpell : BaseEquipSpellCard
{
    private const int PrintedAtk = 8;
    private const int PrintedDefPenalty = -3;
    private const int UpgradedDefPenalty = -4;

    private readonly DuelMonsterAttribute _attribute;
    private readonly YgoCardPackTags _packTags;
    private readonly bool _atkOnly;
    private readonly int _printedAtkOnly;
    private readonly int _upgradedAtkOnly;

    protected FlatAttributeEquipSpell(
        CardRarity rarity,
        DuelMonsterAttribute attribute,
        YgoCardPackTags packTags,
        bool atkOnly = false,
        int printedAtkOnly = 7,
        int upgradedAtkOnly = 10)
        : base(1, rarity, TargetType.Self)
    {
        _attribute = attribute;
        _packTags = YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | packTags;
        _atkOnly = atkOnly;
        _printedAtkOnly = printedAtkOnly;
        _upgradedAtkOnly = upgradedAtkOnly;
    }

    public sealed override YgoCardPackTags PackTags => _packTags;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        _atkOnly
            ? new[] { new DynamicVar("Mgc", (decimal)_printedAtkOnly) }
            : new[] { new DynamicVar("Mgc", PrintedAtk), new DynamicVar("Mgc2", 3m) };

    public sealed override bool CanEquipTo(BaseMonsterCard target) =>
        target.GetEffectiveDuelMonsterAttribute() == _attribute;

    public sealed override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped)
    {
        if (_atkOnly)
            return new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

        int def = IsUpgraded ? UpgradedDefPenalty : PrintedDefPenalty;
        return new StatEffectTotal(
            DynamicVars["Mgc"].BaseValue,
            def);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        if (_atkOnly)
        {
            DynamicVars["Mgc"].BaseValue = _upgradedAtkOnly;
            return;
        }

        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtk, true);
        DynamicVars["Mgc2"].BaseValue = System.Math.Abs(UpgradedDefPenalty);
    }
}

public sealed class Burning_Spear : FlatAttributeEquipSpell
{
    public Burning_Spear() : base(CardRarity.Common, DuelMonsterAttribute.Fire, YgoCardPackTags.Fire | YgoCardPackTags.Burn) { }
}

public sealed class Steel_Shell : FlatAttributeEquipSpell
{
    public Steel_Shell() : base(CardRarity.Common, DuelMonsterAttribute.Water, YgoCardPackTags.Water | YgoCardPackTags.Ocean) { }
}

public sealed class Gust_Fan : FlatAttributeEquipSpell
{
    public Gust_Fan() : base(CardRarity.Common, DuelMonsterAttribute.Wind, YgoCardPackTags.Wind) { }
}

public sealed class Invigoration : FlatAttributeEquipSpell
{
    public Invigoration() : base(CardRarity.Common, DuelMonsterAttribute.Earth, YgoCardPackTags.Earth) { }
}

public sealed class Elf_s_Light : FlatAttributeEquipSpell
{
    public Elf_s_Light() : base(CardRarity.Common, DuelMonsterAttribute.Light, YgoCardPackTags.Light) { }
}

public sealed class Sword_of_Dark_Destruction : FlatAttributeEquipSpell
{
    public Sword_of_Dark_Destruction() : base(CardRarity.Common, DuelMonsterAttribute.Dark, YgoCardPackTags.Dark) { }
}

public sealed class Salamandra : FlatAttributeEquipSpell
{
    public Salamandra() : base(CardRarity.Common, DuelMonsterAttribute.Fire, YgoCardPackTags.Fire | YgoCardPackTags.Burn, atkOnly: true) { }
}

public sealed class Shine_Palace : FlatAttributeEquipSpell
{
    public Shine_Palace() : base(CardRarity.Common, DuelMonsterAttribute.Light, YgoCardPackTags.Light, atkOnly: true) { }
}
