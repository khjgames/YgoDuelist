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

public sealed class Yellow_Luster_Shield : BaseContinuousSpellCard
{
    private const int PrintedDefBonus = 3;
    private int _bonusDef = PrintedDefBonus;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedDefBonus) };

    public Yellow_Luster_Shield()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target)
    {
        if (Owner == null || target.Owner != Owner)
            return StatEffectTotal.None;
        return new StatEffectTotal(0, _bonusDef);
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        _bonusDef = PrintedDefBonus + YgoStatUpgradeScaling.GetSpellTrapStatBonusUpgradeDelta(PrintedDefBonus);
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = _bonusDef;
    }
}
