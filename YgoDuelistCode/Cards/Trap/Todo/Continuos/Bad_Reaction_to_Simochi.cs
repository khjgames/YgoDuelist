using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Bad_Reaction_to_Simochi : BaseContinuousTrapCard, IYgoBadReactionToSimochiHealRedirect
{
    public override bool UseAlternateUpgradedDescription => true;

    public Bad_Reaction_to_Simochi()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self)
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
        typeof(Bad_Reaction_to_Simochi),
    };

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Concat(IsUpgraded ? new[] { CardKeyword.Innate } : Enumerable.Empty<CardKeyword>());

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat(
            IsUpgraded
                ? new[] { HoverTipFactory.FromKeyword(CardKeyword.Innate) }
                : Enumerable.Empty<IHoverTip>());

    decimal IYgoBadReactionToSimochiHealRedirect.GetEnemyHealRedirectDamageMultiplier() => IsUpgraded ? 1.5m : 1m;
}
