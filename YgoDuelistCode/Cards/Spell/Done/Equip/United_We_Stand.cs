using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class United_We_Stand : BaseEquipSpellCard
{
    private const int PrintedPerCard = 4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedPerCard) };

    public United_We_Stand()
        : base(cost: 2, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped)
    {
        Player? owner = equipped.Owner;
        if (owner == null)
            return StatEffectTotal.None;

        int count = YgoEquipSpellFieldStats.CountOwnerFaceUpFieldMonsters(owner)
            + YgoEquipSpellFieldStats.CountOwnerFaceUpSpellTraps(owner);
        int per = (int)DynamicVars["Mgc"].BaseValue;
        int total = per * count;
        return new StatEffectTotal(total, total);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = 5m;
    }
}
