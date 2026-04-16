using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>At start of your turn, enemies with attack intent vs you ≥ <c>Mgc</c> get 1 Weak (see <see cref="YgoDuelist.YgoDuelistCode.Services.YgoGoraTurtleService"/>).</summary>
public sealed class Gora_Turtle : EffectMonsterCard, IYgoTurnStartWeakFromAttackIntent
{
    public Gora_Turtle()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 11,
            baseDef: 11,
            baseMgc: 19,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water;

    public int AttackIntentWeakThreshold => (int)DynamicVars["Mgc"].BaseValue;

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 13m;
    }
}
