using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

/// <summary>Ritual monster; Corpse-Blight on execute kill — <see cref="OnEnemyExecutedByThisAttackAsync"/>.</summary>
public sealed class Shinato_King_of_a_Higher_Plane : RitualMonsterCard
{
    public Shinato_King_of_a_Higher_Plane()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 33,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Ritual | YgoCardPackTags.Light;

    public override Type[] BundledCards => new[] { typeof(Shinato_S_Ark), typeof(Shinato_King_of_a_Higher_Plane) };

    public override Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs) =>
        YgoExecuteKillShared.ApplyHalfBlightToAllEnemiesOnExecuteKillAsync(command, this, cs);
}
