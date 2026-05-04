using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Sword_of_Dragon_S_Soul : BaseEquipSpellCard
{
    public Sword_of_Dragon_S_Soul()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Dragon;

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    public override bool GrantsSplinterDamage => true;

    protected override void OnUpgrade() { }
}
