using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Graveyard: heal <c>Mgc</c> and gain matching Doom through the shared graveyard hook interface.</summary>
public sealed class Skull_Mark_Ladybug : EffectMonsterCard, IYgoOnAddedToYgoGraveyardPile
{
    public Skull_Mark_Ladybug()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 5,
            baseDef: 15,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Heal;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Skull_Mark_Ladybug),
    };

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }

    public async Task OnAddedToYgoGraveyardPileAsync(Player owner, CardPile pile)
    {
        Creature? creature = owner.Creature;
        if (creature == null || !creature.IsAlive)
            return;

        decimal amount = DynamicVars["Mgc"].BaseValue;
        if (amount <= 0m)
            return;

        await CreatureCmd.Heal(creature, amount);
        await PowerCmd.Apply<DoomPower>(creature, amount, creature, this);
    }
}
