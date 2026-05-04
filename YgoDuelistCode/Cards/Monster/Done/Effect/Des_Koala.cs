using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Des_Koala : EffectMonsterCard, IMonsterFlipEffect
{
    public Des_Koala()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 11,
            baseDef: 18,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Des_Koala) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Des_Koala || Owner?.Creature?.CombatState == null)
            return;

        int hits = YgoPlayerPiles.Hand(Owner)?.Cards.Count ?? 0;
        if (hits <= 0)
            return;

        decimal dmg = DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0m)
            return;

        foreach (Creature enemy in YgoMpCombatOrder.CreatureListOrderedByCombatId(Owner.Creature.CombatState.HittableEnemies))
        {
            if (!enemy.IsAlive)
                continue;
            for (int i = 0; i < hits; i++)
                await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 3m;
    }

}
