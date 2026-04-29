using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Corpse-Blight on execute kill — same rule as Shinato.</summary>
public sealed class Des_Volstgalph : EffectMonsterCard
{
    public Des_Volstgalph()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 22,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Dragon | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Des_Volstgalph) };

    public override Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs) =>
        YgoExecuteKillShared.ApplyHalfBlightToAllEnemiesOnExecuteKillAsync(command, this, cs);
}
