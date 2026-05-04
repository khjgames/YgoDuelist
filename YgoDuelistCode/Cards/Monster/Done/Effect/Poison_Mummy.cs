using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Poison_Mummy : EffectMonsterCard, IMonsterFlipEffect
{
    public Poison_Mummy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 10,
            baseDef: 18,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Poison_Mummy),
    };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Poison_Mummy || Owner?.Creature?.CombatState == null)
            return;

        decimal dmg = DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0)
            return;

        CombatState cs = Owner.Creature.CombatState;
        List<Creature> enemies = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs);
        if (enemies.Count == 0)
            return;

        Creature? target = enemies.Count == 1
            ? enemies[0]
            : YgoDeterministicRng.PickOne(cs, enemies, "POISON_MUMMY-FLIP");

        if (target == null || !target.IsAlive)
            return;

        await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 8m;
    }
}
