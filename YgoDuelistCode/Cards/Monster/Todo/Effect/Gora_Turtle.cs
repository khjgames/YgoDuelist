using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>At start of your turn, enemies with attack intent vs you ≥ <c>Mgc</c> get 1 Weak (owner turn-start field-monster hook).</summary>
public sealed class Gora_Turtle : EffectMonsterCard, IYgoTurnStartWeakFromAttackIntent, IYgoOwnerTurnStartFieldMonsterEffect
{
    public Gora_Turtle()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 11,
            baseDef: 11,
            baseMgc: 19,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water;

    public int AttackIntentWeakThreshold => (int)DynamicVars["Mgc"].BaseValue;

    public bool IsOwnerTurnStartFieldMonsterEffectActive() => !FaceDown;

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 13m;
    }

    public async Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        if (owner?.Creature?.CombatState == null || FaceDown)
            return;
        int threshold = AttackIntentWeakThreshold;
        if (threshold <= 0)
            return;

        var ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        foreach (Creature enemy in owner.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive))
        {
            int intent = YgoIntentAttackDamage.GetTotalAttackIntentDamage(enemy, owner.Creature);
            if (intent < threshold)
                continue;
            await PowerCmd.Apply<WeakPower>(enemy, 1m, owner.Creature, this);
        }
    }
}
