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
