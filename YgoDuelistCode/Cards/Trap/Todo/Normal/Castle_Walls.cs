using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Castle_Walls : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar("Mgc", 5m),
        };

    public Castle_Walls()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
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
        typeof(Castle_Walls),
    };

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        await PowerCmd.Apply<CastleWallsPower>(Owner.Creature, DynamicVars["Mgc"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(3m);
    }
}
