using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class King_Tiger_Wanghu : EffectMonsterCard, IMonsterActivatedEffect
{
    public King_Tiger_Wanghu()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 17,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-KING_TIGER_WANGHU.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            decimal cap = GetCurrentAttackThreshold(Owner);
            return Owner.Creature.CombatState.HittableEnemies.Any(e =>
                e.IsAlive && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, Owner.Creature) > 0
                && YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, Owner.Creature) < cap);
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Creature? playerCreature = source.Owner?.Creature ?? cardPlay.Card?.Owner?.Creature;
        if (playerCreature == null || cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, source.Owner ?? cardPlay.Card?.Owner);
        if (pet == null)
            return;

        int intent = YgoIntentAttackDamage.GetTotalAttackIntentDamage(cardPlay.Target, playerCreature);
        decimal cap = GetCurrentAttackThreshold(source.Owner);
        if (intent <= 0 || intent >= cap)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, 1m, pet, source);
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, 1m, pet, source);
    }

    private decimal GetCurrentAttackThreshold(MegaCrit.Sts2.Core.Entities.Players.Player? player)
    {
        if (player == null)
            return DynamicVars.Damage.BaseValue;

        var fieldCards = DuelMonsterFieldRegistry.GetFieldMonsters(player).ToList();
        if (!fieldCards.Contains(this))
            fieldCards.Add(this);
        return CalcDuelMonsterStats(fieldCards).Atk;
    }
}
