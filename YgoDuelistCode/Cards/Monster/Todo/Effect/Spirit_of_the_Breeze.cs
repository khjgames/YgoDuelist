using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Spirit_of_the_Breeze : EffectMonsterCard
{
    public Spirit_of_the_Breeze()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 0,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override Task OnGraveyardRelicAfterAttackOpeningAsync(
        AttackCommand command,
        Player? attackingPlayer,
        BlockingPlayerChoiceContext ctx)
    {
        if (attackingPlayer?.Creature == null)
            return Task.CompletedTask;
        return CreatureCmd.Heal(attackingPlayer.Creature, 1m);
    }
}
