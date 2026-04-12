using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

public sealed class Insect_Armor_with_Laser_Cannon : BaseEquipSpellCard
{
    public Insect_Armor_with_Laser_Cannon()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Insect;

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    public override bool GrantsSplinterTo(BaseMonsterCard equipped) =>
        equipped.DuelMonsterRace == DuelMonsterRace.Beast
        || equipped.DuelMonsterRace == DuelMonsterRace.BeastWarrior
        || equipped.DuelMonsterRace == DuelMonsterRace.WingedBeast;

    protected override bool CardShowsSplinterKeywordHint => true;

    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}
