using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Dark_Spirit_of_the_Silent : BaseTrapCard
{
    private static readonly LocString DoubleHitPrompt = new("combat_messages", "DARK_SPIRIT_PICK_DOUBLE_HIT");

    public Dark_Spirit_of_the_Silent()
        : base(cost: 2, rarity: CardRarity.Uncommon, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || Owner?.Creature?.CombatState == null)
                return false;
            return CountEnemiesWithAttackIntent(Owner) >= 2;
        }
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<Creature> withAttack = cs.HittableEnemies
            .Where(c => c.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(c, Owner.Creature) > 0)
            .ToList();
        if (withAttack.Count < 2)
            return;

        Creature? stunned = cardPlay.Target;
        if (stunned == null || !stunned.IsAlive || !withAttack.Contains(stunned))
            return;

        List<Creature> secondPool = withAttack.Where(c => c != stunned).ToList();
        if (secondPool.Count == 0)
            return;

        Creature doubleHit;
        if (secondPool.Count == 1)
        {
            doubleHit = secondPool[0];
        }
        else
        {
            var ctx = new BlockingPlayerChoiceContext();
            var prefs = new CardSelectorPrefs(DoubleHitPrompt, 1, 1) { Cancelable = true };
            List<YgoEnemyIntentProxyCard> proxies = secondPool.Select(c => new YgoEnemyIntentProxyCard(c)).ToList();
            IEnumerable<CardModel> pick;
            try
            {
                pick = await CardSelectCmd.FromSimpleGrid(ctx, proxies, Owner, prefs);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            YgoEnemyIntentProxyCard? proxy = pick.OfType<YgoEnemyIntentProxyCard>().FirstOrDefault();
            if (proxy?.TargetCreature is not { } hit || !hit.IsAlive || !secondPool.Contains(hit))
                return;
            doubleHit = hit;
        }

        await CreatureCmd.Stun(stunned);
        YgoDarkSpiritSilentState.Activate(Owner, stunned, doubleHit);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private static int CountEnemiesWithAttackIntent(Player player)
    {
        Creature? pc = player.Creature;
        CombatState? cs = pc?.CombatState;
        if (cs == null)
            return 0;
        return cs.HittableEnemies.Count(e => e.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) > 0);
    }
}
