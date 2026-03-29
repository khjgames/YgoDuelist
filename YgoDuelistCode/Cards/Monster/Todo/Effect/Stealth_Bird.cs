using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Stealth_Bird : EffectMonsterCard
{
    public Stealth_Bird()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 7,
            baseDef: 17,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }

    /// <summary>
    /// Flip Summon / flip-to-attack from face-down defense: magic damage to the chosen enemy
    /// (<see cref="Command.Command_Change_Battle_Position"/> or <see cref="Command.Command_Attack"/>).
    /// </summary>
    public static async Task DealFlipSummonDamageIfEligibleAsync(
        PlayerChoiceContext choiceContext,
        Stealth_Bird bird,
        bool wasFaceDownDefenseBeforePositionChange,
        Creature? target,
        Creature? playerCreature)
    {
        if (!wasFaceDownDefenseBeforePositionChange || target == null || !target.IsAlive || playerCreature == null)
            return;

        decimal dmg = bird.DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0m)
            return;

        await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, playerCreature, bird);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 15m;
    }
}
