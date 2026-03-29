using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>When destroyed by battle: apply <c>{Mgc}</c> <see cref="BlightPower"/> to all enemies.</summary>
public sealed class Yomi_Ship : EffectMonsterCard
{
    public Yomi_Ship()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 8,
            baseDef: 14,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    internal static async Task ApplyBlightWhenDestroyedByBattleAsync(Player player, Yomi_Ship card)
    {
        if (player.Creature?.CombatState == null)
            return;

        decimal stacks = card.DynamicVars["Mgc"].BaseValue;
        foreach (Creature enemy in player.Creature.CombatState.HittableEnemies)
        {
            if (!enemy.IsAlive)
                continue;
            await PowerCmd.Apply<BlightPower>(enemy, stacks, player.Creature, card);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 9m;
    }
}
