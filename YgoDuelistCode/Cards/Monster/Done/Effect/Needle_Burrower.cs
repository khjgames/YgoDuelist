using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Corpse-Blight on execute kill — same rule as <see cref="Des_Volstgalph"/> / <see cref="Shinato_King_of_a_Higher_Plane"/>.</summary>
public sealed class Needle_Burrower : EffectMonsterCard
{
    public Needle_Burrower()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 17,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Insect | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Needle_Burrower) };

    public override Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs) =>
        YgoExecuteKillShared.ApplyHalfBlightToAllEnemiesOnExecuteKillAsync(command, this, cs);
}
