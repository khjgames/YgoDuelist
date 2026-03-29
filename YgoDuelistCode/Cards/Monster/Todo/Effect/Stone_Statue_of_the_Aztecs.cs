using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>
/// On block from this card’s DEF: enemies whose current attack intent to you is less than that block take
/// <c>Mgc</c> × (block − intent) magic damage (<c>Mgc</c> 2, upgrades to 3).
/// </summary>
public sealed class Stone_Statue_of_the_Aztecs : EffectMonsterCard
{
    public Stone_Statue_of_the_Aztecs()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 3,
            baseDef: 20,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }

    protected override async Task OnAfterGainBlockFromCombatActionAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        int blockGranted)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Creature playerCreature = Owner.Creature;
        var cs = playerCreature.CombatState;
        decimal mult = DynamicVars["Mgc"].BaseValue;
        if (mult <= 0m || blockGranted <= 0)
            return;

        foreach (Creature enemy in cs.HittableEnemies.Where(e => e.IsAlive))
        {
            int intent = YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, playerCreature);
            int diff = blockGranted - intent;
            if (diff <= 0)
                continue;

            decimal dmg = mult * diff;
            if (dmg <= 0m)
                continue;

            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, playerCreature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
