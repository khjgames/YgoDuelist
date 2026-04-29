using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Rare_Fish : FusionMonsterCard
{
    public override bool BulkBundled => true;

    public Rare_Fish()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 15,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fish,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion.Fusionist),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal.Enchanting_Mermaid))
    {
    }
}
