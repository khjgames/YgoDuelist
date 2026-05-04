using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class D_D_Crazy_Beast : EffectMonsterCard
{
    public D_D_Crazy_Beast()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 14,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Earth;

    public override int PermanentAtkDeltaOnEnemyExecute => IsUpgraded ? 1 : 2;

    protected override void OnUpgrade() => base.OnUpgrade();
}
