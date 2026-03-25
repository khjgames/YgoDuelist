using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Zone_Eater : EffectMonsterCard
{
    public Zone_Eater()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 2,
            baseDef: 2,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<Creature> enemies = YgoDeterministicRng
            .StableOrder(cs.HittableEnemies.Where(c => c.IsAlive), c => c.CombatId)
            .ToList();

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
            target = YgoDeterministicRng.PickOne(cs, enemies, $"ZONE_EATER_SUMMON-{Id.Entry}");
        }

        if (target == null || !target.IsAlive)
            return;

        ZoneEaterMarkPower? existing = target.GetPower<ZoneEaterMarkPower>();
        if (existing != null)
            await PowerCmd.Remove(existing);

        await PowerCmd.Apply<ZoneEaterMarkPower>(target, 5m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
