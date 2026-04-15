using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Ultimate_Obedient_Fiend : EffectMonsterCard
{
    public Ultimate_Obedient_Fiend()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 35,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend,
            duelMonsterAttackPlayEnergyOverride: 0)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Fire | YgoCardPackTags.Fiend;

    /// <summary>
    /// Attack from hand (summon+combat) and field Command Attack require an empty hand and no other monsters on the field.
    /// Defense / Set and Command Defend ignore this.
    /// </summary>
    public static bool IsAttackPlayAllowed(Player? player, Ultimate_Obedient_Fiend selfCard)
    {
        if (player?.PlayerCombatState == null)
            return false;

        List<BaseMonsterCard> field = DuelMonsterFieldRegistry.GetFieldMonsters(player)?.ToList() ?? new List<BaseMonsterCard>();
        foreach (BaseMonsterCard? m in field)
        {
            if (m != null && !ReferenceEquals(m, selfCard))
                return false;
        }

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return true;

        bool selfInHand = hand.Cards.Any(c => ReferenceEquals(c, selfCard));
        if (selfInHand)
            return hand.Cards.Count == 1;

        return hand.Cards.Count == 0;
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;
            if (Owner == null || Type != CardType.Attack)
                return true;
            return IsAttackPlayAllowed(Owner, this);
        }
    }

    public override bool IsCommandAttackPlayable(Player? owner, Creature? pet) =>
        owner == null || IsAttackPlayAllowed(owner, this);
}
