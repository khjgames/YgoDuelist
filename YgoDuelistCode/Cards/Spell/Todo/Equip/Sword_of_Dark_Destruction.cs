using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

public sealed class Sword_of_Dark_Destruction : BaseEquipSpellCard
{
    public Sword_of_Dark_Destruction()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    //public override YgoCardPackTags PackTags =>
    //    YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;


    protected override void OnUpgrade()
    {
    }
}
