using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Dark_Jeroid : EffectMonsterCard
{
    public Dark_Jeroid()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 12,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    /// <inheritdoc cref="BaseMonsterCard.NonAttackPlayTargetType" />
    /// Summon applies Weak to an enemy (chosen target or deterministic pick).
    protected override TargetType NonAttackPlayTargetType => TargetType.AnyEnemy;

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | FusionMonsterCard.PackTagsForFusionProfile(DuelMonsterAttribute, DuelMonsterRace);

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<Creature> enemies = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs);

        if (enemies.Count == 0)
            return;

        Creature? target = null;
        if (cardPlay.Target != null
            && cardPlay.Target.Side == CombatSide.Enemy
            && cardPlay.Target.IsAlive
            && enemies.Contains(cardPlay.Target))
        {
            target = cardPlay.Target;
        }
        else if (enemies.Count == 1)
        {
            target = enemies[0];
        }
        else
        {
            target = YgoDeterministicRng.PickOne(cs, enemies, $"DARK_JEROID_SUMMON-{Id.Entry}");
        }

        if (target == null || !target.IsAlive)
            return;

        await PowerCmd.Apply<WeakPower>(target, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
