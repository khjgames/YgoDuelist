using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Bad_Reaction_to_Simochi : BaseContinuousTrapCard
{
    public Bad_Reaction_to_Simochi()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

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
}
