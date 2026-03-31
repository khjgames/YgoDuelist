using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Mudora : EffectMonsterCard
{
    public Mudora()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 15,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Mudora),
    };

    /// <summary>+2 ATK for each Fairy-Type monster card in your Graveyard.</summary>
    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return (0, 0);

        int n = GraveyardRelic.GetGraveyardCards(Owner).Count(c =>
            c is BaseMonsterCard m && m.DuelMonsterRace == DuelMonsterRace.Fairy);

        return (2 * n, 0);
    }
}
