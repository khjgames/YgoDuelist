using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Field;

/// <summary>
/// +5 ATK / −4 DEF for one attribute; upgraded: ATK uses spell/trap bonus scaling, DEF penalty −3 (Mgc2 magnitude 4→3).
/// </summary>
public abstract class FlatAttributeFieldSpell : BaseFieldSpellCard
{
    private const int PrintedAtk = 5;
    private const int PrintedDefPenalty = -4;
    private const int UpgradedDefPenalty = -3;

    private readonly DuelMonsterAttribute _attribute;
    private readonly YgoCardPackTags _packTags;

    protected FlatAttributeFieldSpell(DuelMonsterAttribute attribute, YgoCardPackTags packTags)
        : base(1, CardRarity.Common, TargetType.Self)
    {
        _attribute = attribute;
        _packTags = YgoCardPackTags.Starter | YgoCardPackTags.Spell | packTags;
    }

    public sealed override YgoCardPackTags PackTags => _packTags;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", PrintedAtk), new DynamicVar("Mgc2", 4m) };

    public sealed override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target) =>
        target.DuelMonsterAttribute == _attribute
            ? new StatEffectTotal(
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtk, IsUpgraded),
                IsUpgraded ? UpgradedDefPenalty : PrintedDefPenalty)
            : StatEffectTotal.None;

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtk, true);
        DynamicVars["Mgc2"].BaseValue = System.Math.Abs(UpgradedDefPenalty);
    }
}

public sealed class Umiiruka : FlatAttributeFieldSpell
{
    public Umiiruka() : base(DuelMonsterAttribute.Water, YgoCardPackTags.Water | YgoCardPackTags.Ocean) { }
}

public sealed class Molten_Destruction : FlatAttributeFieldSpell
{
    public Molten_Destruction() : base(DuelMonsterAttribute.Fire, YgoCardPackTags.Fire | YgoCardPackTags.Burn) { }
}

public sealed class Gaia_Power : FlatAttributeFieldSpell
{
    public Gaia_Power() : base(DuelMonsterAttribute.Earth, YgoCardPackTags.Earth) { }
}

public sealed class Rising_Air_Current : FlatAttributeFieldSpell
{
    public Rising_Air_Current() : base(DuelMonsterAttribute.Wind, YgoCardPackTags.Wind) { }
}

public sealed class Mystic_Plasma_Zone : FlatAttributeFieldSpell
{
    public Mystic_Plasma_Zone() : base(DuelMonsterAttribute.Dark, YgoCardPackTags.Dark) { }
}

public sealed class Luminous_Spark : FlatAttributeFieldSpell
{
    public Luminous_Spark() : base(DuelMonsterAttribute.Light, YgoCardPackTags.Light) { }
}
