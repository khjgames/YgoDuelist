using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

public sealed class Cyber_Shield : BaseEquipSpellCard
{
    public Cyber_Shield()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    //public override YgoCardPackTags PackTags =>
    //     YgoCardPackTags.Spell | YgoCardPackTags.Machine;

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;


    protected override void OnUpgrade()
    {
    }
}
