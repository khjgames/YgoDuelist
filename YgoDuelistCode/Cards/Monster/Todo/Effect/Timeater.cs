using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Timeater : EffectMonsterCard
{
    public Timeater()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 19,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Timeater),
    };

    public override async Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs)
    {
        bool executed = false;
        foreach (DamageResult r in command.Results)
        {
            if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                continue;
            executed = true;
            break;
        }

        if (!executed)
            return;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive).ToList())
            await CreatureCmd.Stun(e);
    }
}
