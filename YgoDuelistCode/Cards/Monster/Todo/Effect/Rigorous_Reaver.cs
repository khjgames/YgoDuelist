using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>When destroyed by battle: apply 1 Weak, 1 Vulnerable, and permanent -1 Strength / Dexterity (via <see cref="RigorousReaverPower"/>) to a chosen enemy.</summary>
public sealed class Rigorous_Reaver : EffectMonsterCard
{
    public Rigorous_Reaver()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 16,
            baseDef: 3,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Rigorous_Reaver),
    };

    /// <summary>
    /// If there are 2+ enemies, pick uniformly at random among the two with the highest MaxHp.
    /// Otherwise the sole enemy is targeted.
    /// </summary>
    internal static async Task ApplyWhenDestroyedByBattleAsync(Player player, Rigorous_Reaver card)
    {
        Creature? playerCreature = player.Creature;
        CombatState? cs = playerCreature?.CombatState;
        if (cs == null)
            return;

        List<Creature> ordered = YgoDeterministicRng
            .StableOrder(cs.HittableEnemies.Where(e => e.IsAlive), e => e.CombatId)
            .OrderByDescending(e => e.MaxHp)
            .ToList();

        if (ordered.Count == 0)
            return;

        Creature target = ordered.Count == 1
            ? ordered[0]
            : YgoDeterministicRng.PickOne(cs, ordered.Take(2).ToList(), "RIGOROUS_REAVER-TARGET")!;

        if (!target.IsAlive)
            return;

        await PowerCmd.Apply<WeakPower>(target, 1m, playerCreature, card);
        await PowerCmd.Apply<VulnerablePower>(target, 1m, playerCreature, card);
        await PowerCmd.Apply<StrengthPower>(target, -1m, playerCreature, card, silent: true);
        await PowerCmd.Apply<DexterityPower>(target, -1m, playerCreature, card, silent: true);
        await PowerCmd.Apply<RigorousReaverPower>(target, 1m, playerCreature, card);
    }
}
