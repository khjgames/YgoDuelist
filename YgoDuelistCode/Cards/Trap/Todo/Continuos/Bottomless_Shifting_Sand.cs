using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Bottomless_Shifting_Sand : BaseContinuousTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", 4m),
            new DynamicVar("Mgc2", 25m)
        };

    public Bottomless_Shifting_Sand()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Bottomless_Shifting_Sand),
    };

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(-1m);
        DynamicVars["Mgc2"].UpgradeValueBy(15m);
    }

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;
}
