using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Lightning_Blade : BaseEquipSpellCard
{
    private const int PrintedAtk = 8;
    private const int PrintedWaterPenalty = -3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", PrintedAtk), new DynamicVar("Mgc2", 3m) };

    public Lightning_Blade()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Warrior;

    public override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == DuelMonsterRace.Warrior;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

    public override StatEffectTotal GetGlobalFieldStatEffect(BaseMonsterCard target)
    {
        if (target.GetEffectiveDuelMonsterAttribute() != DuelMonsterAttribute.Water)
            return StatEffectTotal.None;

        int penalty = IsUpgraded ? -4 : PrintedWaterPenalty;
        return new StatEffectTotal(penalty, 0);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtk, true);
        DynamicVars["Mgc2"].BaseValue = 4m;
    }
}
