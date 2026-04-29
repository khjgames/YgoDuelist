using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

public sealed class Gatling_Dragon : FusionMonsterCard, IMonsterActivatedEffect
{
    private const string CoinSalt = "GATLING_DRAGON-COIN";

    public override int AttackPortionCount => 5;

    public Gatling_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 26,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine,
            typeof(Barrel_Dragon),
            typeof(Blowback_Dragon))
    {
    }

    public override float PackWeightMultiplier => 1.13f;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Machine | YgoCardPackTags.Chance;

    public override Type[] RelatedCards => new[] { typeof(Gatling_Dragon), typeof(Barrel_Dragon), typeof(Blowback_Dragon) };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", 3m) });

    public int ActivatedEffectEnergyCost =>
        YgoMonsterCommandEnergyModifiers.GetFieldCommandAttackEnergyCost(this);

    public CardType ActivatedEffectCardType => CardType.Attack;

    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GATLING_DRAGON.activated_effect.description";

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner;
        if (player?.Creature?.CombatState is not { } cs)
            return;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null || cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        int flips = (int)source.DynamicVars["Mgc2"].BaseValue;
        if (flips <= 0)
            return;

        int heads = 0;
        for (int i = 0; i < flips; i++)
        {
            ulong mix = YgoDeterministicRng.MixDuelMonsterAttack(player.Creature, pet, cardPlay) ^ (ulong)(i + 101);
            if (YgoDeterministicRng.CoinFlip(cs, CoinSalt, mix))
                heads++;
        }

        if (heads <= 0)
        {
            MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
            return;
        }

        decimal halfAtk = NormalMonsterCard.GetTotalAtkForPreview(source) * 0.5m;
        decimal total = halfAtk * heads;
        if (total > 0m)
        {
            await DamageCmd.Attack(total)
                .FromCard(source)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc2"].BaseValue = 4m;
    }
}
