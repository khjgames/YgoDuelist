using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;

public sealed class Poisonous_Snake_Token : YgoTokenEffectMonster
{
    public Poisonous_Snake_Token()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Reptile)
    {
    }

    public override Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        if (ctx.CommandState?.DestroyedByEnemyBattleDamage == true)
            TaskHelper.RunSafely(ApplyWhenDestroyedByBattleAsync(ctx.Player, ctx.CommandState.BattleDamageKillerEnemy));
        return base.OnPetDiedAfterOptionPileHandlingAsync(ctx);
    }

    /// <summary>500 LP in YGO → 5 damage at this scaling.</summary>
    public static async Task ApplyWhenDestroyedByBattleAsync(Player player, Creature? killerEnemy)
    {
        if (player.Creature?.CombatState == null)
            return;
        var ctx = new BlockingPlayerChoiceContext();
        foreach (Creature e in player.Creature.CombatState.HittableEnemies)
        {
            if (e.IsAlive)
                await CreatureCmd.Damage(ctx, e, 5m, ValueProp.Unpowered, player.Creature, null);
        }
    }
}
