using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Graveyard: shuffle into deck + draw — <see cref="YgoOutstandingDogMarronGraveyard"/>.</summary>
public sealed class Outstanding_Dog_Marron : EffectMonsterCard
{
    public Outstanding_Dog_Marron()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 1,
            baseDef: 1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Outstanding_Dog_Marron) };
}
