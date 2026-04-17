using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>When sent from hand to the Graveyard, deal 20 to an enemy (deterministic random when several hittable enemies exist).</summary>
public sealed class Elephant_Statue_of_Disaster : EffectMonsterCard
{
    private static readonly Dictionary<CombatState, int> s_seq = new();
    private static readonly object s_seqLock = new();

    public Elephant_Statue_of_Disaster()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 15,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        if (from != PileType.Hand)
            return;
        TaskHelper.RunSafely(RunHandToGraveyardDamageAsync());
    }

    private async Task RunHandToGraveyardDamageAsync()
    {
        Player? player = Owner;
        Creature? pc = player?.Creature;
        CombatState? cs = pc?.CombatState;
        if (cs == null)
            return;

        List<Creature> enemies = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
            return;

        Creature? target = enemies.Count == 1
            ? enemies[0]
            : YgoDeterministicRng.PickOne(cs, enemies, $"ELEPHANT_STATUE_DISASTER_GY-{NextSeq(cs)}");

        if (target == null || !target.IsAlive)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        await DamageCmd.Attack(20m)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
    }

    private static int NextSeq(CombatState cs)
    {
        lock (s_seqLock)
        {
            s_seq.TryGetValue(cs, out int n);
            int next = n + 1;
            s_seq[cs] = next;
            return next;
        }
    }
}
