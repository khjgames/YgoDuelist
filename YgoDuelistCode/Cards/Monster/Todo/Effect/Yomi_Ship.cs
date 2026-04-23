using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>When destroyed by battle: apply <c>{Mgc}</c> <see cref="BlightPower"/> to all enemies.</summary>
public sealed class Yomi_Ship : EffectMonsterCard
{
    public Yomi_Ship()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 8,
            baseDef: 14,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Aqua)
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
        typeof(Yomi_Ship),
    };

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;

    public override async Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        if (ctx.CommandState?.DestroyedByEnemyBattleDamage == true)
            await ApplyBlightWhenDestroyedByBattleAsync(ctx.Player, this);
        await base.OnPetDiedAfterOptionPileHandlingAsync(ctx);
    }

    internal static async Task ApplyBlightWhenDestroyedByBattleAsync(Player player, Yomi_Ship card)
    {
        if (player.Creature?.CombatState == null)
            return;

        decimal stacks = card.DynamicVars["Mgc"].BaseValue;
        foreach (Creature enemy in YgoMpCombatOrder.CreatureListOrderedByCombatId(player.Creature.CombatState.HittableEnemies))
        {
            if (!enemy.IsAlive)
                continue;
            await PowerCmd.Apply<BlightPower>(enemy, stacks, player.Creature, card);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 9m;
    }
}
