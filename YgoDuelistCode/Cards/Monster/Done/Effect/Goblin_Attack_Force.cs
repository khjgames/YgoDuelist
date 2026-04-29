using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Goblin_Attack_Force : EffectMonsterCard
{
    public override int AttackPortionCount => 3;
    public Goblin_Attack_Force()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 23,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            duelMonsterAttackPlayEnergyOverride: 1)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Warrior | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Goblin_Attack_Force) };

    protected override async Task OnAfterMonsterAttackHitAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        AttackCommand attackCommand)
    {
        await base.OnAfterMonsterAttackHitAsync(choiceContext, cardPlay, attackCommand);
        if (Owner == null)
            return;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
        if (pet == null)
            return;

        await ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync(choiceContext, Owner, attackPosition: false);
        MonsterCommandRegistry.GetOrCreate(pet).KeepCommandLockOnNextTurnStart = true;
    }
}
