using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Activate Effect energy matches Command Attack energy minus 1. Toss a coin 3 times: if at least 2 are heads, deal damage equal to this card's ATK to target enemy; otherwise this monster takes {Mgc} damage.
/// </summary>
public sealed class Blowback_Dragon : EffectMonsterCard, IMonsterActivatedEffect
{
    private const string CoinSalt = "BLOWBACK_DRAGON-COIN";

    public Blowback_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 23,
            baseDef: 12,
            baseMgc: 6,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Machine | YgoCardPackTags.Chance;

    public override Type[] RelatedCards => new[] { typeof(Blowback_Dragon), typeof(Barrel_Dragon) };

    public int ActivatedEffectEnergyCost =>
        Math.Max(0, YgoMonsterCommandEnergyModifiers.GetFieldCommandAttackEnergyCost(this) - 1);

    public CardType ActivatedEffectCardType => CardType.Attack;

    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-BLOWBACK_DRAGON.activated_effect.description";

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner;
        if (player?.Creature?.CombatState is not { } cs)
            return;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null || cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        int heads = 0;
        for (int i = 0; i < 3; i++)
        {
            ulong mix = YgoDeterministicRng.MixDuelMonsterAttack(player.Creature, pet, cardPlay) ^ (ulong)(i + 7);
            if (YgoDeterministicRng.CoinFlip(cs, CoinSalt, mix))
                heads++;
        }

        if (heads >= 2)
        {
            decimal dmg = NormalMonsterCard.GetTotalAtkForPreview(source);
            if (dmg > 0m)
            {
                await DamageCmd.Attack(dmg)
                    .FromCard(source)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            }
        }
        else if (player.Creature != null)
        {
            decimal self = source.DynamicVars["Mgc"].BaseValue;
            if (self > 0m)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    pet,
                    self,
                    ValueProp.Move | ValueProp.Unpowered,
                    dealer: null,
                    cardSource: source);
            }
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
    }
}
