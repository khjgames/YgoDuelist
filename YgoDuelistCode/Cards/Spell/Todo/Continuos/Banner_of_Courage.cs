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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Banner_of_Courage : BaseContinuousSpellCard
{
    private const int PrintedAtkBonus = 2;
    private int _bonusAtk = PrintedAtkBonus;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkBonus) };

    public Banner_of_Courage()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Banner_of_Courage),
    };

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target)
    {
        if (Owner == null || target.Owner != Owner)
            return StatEffectTotal.None;
        return new StatEffectTotal(_bonusAtk, 0);
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        _bonusAtk = PrintedAtkBonus + YgoStatUpgradeScaling.GetSpellTrapStatBonusUpgradeDelta(PrintedAtkBonus);
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = _bonusAtk;
    }

}
