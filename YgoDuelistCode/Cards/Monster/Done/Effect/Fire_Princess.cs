using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Fire_Princess : EffectMonsterCard, IYgoAfterOwnerPlayerCreatureHealGain
{
    public Fire_Princess()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 13,
            baseDef: 15,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Heal;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Fire_Princess),
    };

    public async Task ReactToOwnerPlayerHpGainAfterHealAsync(
        Player owningPlayer,
        Creature healedCreature,
        decimal hpBeforeHeal,
        decimal gainedHp,
        CombatState combatState,
        int deterministicTick)
    {
        _ = hpBeforeHeal;
        if (gainedHp <= 0m || FaceDown || Owner?.PlayerCombatState == null)
            return;

        List<Creature> enemies = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(combatState);
        if (enemies.Count == 0)
            return;

        if (!YgoMpCombatOrder.PetsAny(
                Owner.PlayerCombatState,
                p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, this)))
            return;

        decimal dmg = DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0m)
            return;

        Creature? target = enemies.Count == 1
            ? enemies[0]
            : YgoDeterministicRng.PickOne(combatState, enemies, $"FIRE_PRINCESS-{Id.Entry}-{deterministicTick}");

        if (target == null || !target.IsAlive)
            return;

        var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
        await DamageCmd.Attack(dmg)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(ctx);
    }
}
