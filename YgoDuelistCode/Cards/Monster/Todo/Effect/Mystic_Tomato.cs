using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Battle-death deck search — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoMysticTomatoGraveyard"/>.</summary>
public sealed class Mystic_Tomato : EffectMonsterCard
{
    public Mystic_Tomato()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 14,
            baseDef: 11,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Normal;

    public override Type[] RelatedCards => new[] { typeof(Mystic_Tomato) };
}
