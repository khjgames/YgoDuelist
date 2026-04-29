using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Bowganian : EffectMonsterCard
{
    public Bowganian()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 13,
            baseDef: 10,
            baseMgc: 6,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Bowganian),
    };

    /// <inheritdoc cref="BaseMonsterCard.NonAttackPlayTargetType" />
    /// Annual effect deals damage to an enemy (target or deterministic).
    protected override TargetType NonAttackPlayTargetType => TargetType.AnyEnemy;

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (!YgoAnnualTracker.TryConsumeAnnual(Owner, "BOWGANIAN"))
            return;

        var target = cardPlay.Target;
        if (target == null)
        {
            var cs = Owner.Creature.CombatState;
            var list = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs);
            if (list.Count == 0)
                return;
            target = YgoDeterministicRng.PickOne(cs, list, "BOWGANIAN-RANDOM_TARGET");
        }

        if (target != null)
        {
            await DamageCmd.Attack(DynamicVars["Mgc"].BaseValue)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 9m;
    }
}
