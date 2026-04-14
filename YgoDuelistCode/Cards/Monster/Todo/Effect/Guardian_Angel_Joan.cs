using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>On execute kill: heal <c>Mgc</c> + 2% of that enemy's max HP.</summary>
public sealed class Guardian_Angel_Joan : EffectMonsterCard
{
    public Guardian_Angel_Joan()
        : base(
            cost: 1,
            type: global::MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack,
            rarity: global::MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Uncommon,
            target: global::MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterAttribute.Light,
            baseAtk: 28,
            baseDef: 20,
            baseMgc: 1,
            duelMonsterRace: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Heal;

    public override Type[] RelatedCards => new[] { typeof(Guardian_Angel_Joan) };

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
