using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
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

public sealed class D_D_Warrior : EffectMonsterCard
{
    public D_D_Warrior()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | FusionMonsterCard.PackTagsForFusionProfile(DuelMonsterAttribute, DuelMonsterRace);

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Attack;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-D_D_WARRIOR.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this);
            return pet != null
                && MonsterCommandRegistry.TryGet(pet, out MonsterCommandState s)
                && s.WarriorBanishWindowActive;
        }
    }

    public override Task OnGraveyardRelicAfterAttackOpeningAsync(
        AttackCommand command,
        Player? attackingPlayer,
        BlockingPlayerChoiceContext ctx)
    {
        Creature? wlPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this);
        if (wlPet != null)
            MonsterCommandRegistry.GetOrCreate(wlPet).WarriorBanishWindowActive = true;
        return Task.CompletedTask;
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner;
        Creature? target = cardPlay.Target;
        if (player == null || target == null || target.Side != CombatSide.Enemy)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source);
        if (pet == null)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
        MonsterCommandRegistry.GetOrCreate(pet).WarriorBanishWindowActive = false;

        await CreatureCmd.Kill(pet, force: true);
        await YgoBanishedService.BanishCard(player, source);

        await PowerCmd.Apply<WeakPower>(target, DynamicVars["Mgc"].BaseValue, player.Creature, source);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}