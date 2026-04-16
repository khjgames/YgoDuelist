using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Total_Defense_Shogun : EffectMonsterCard, IYgoDeferredBlockFromDefendCommand
{
    public Total_Defense_Shogun()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 15,
            baseDef: 25,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Warrior | YgoCardPackTags.Dark;

    public override Type[] RelatedCards => new[] { typeof(Total_Defense_Shogun) };

    public int GetDeferredBlockForDefendCommand() => (int)(NormalMonsterCard.GetTotalDefForPreview(this) / 5m);
}
