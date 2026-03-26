using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;

public sealed class Forest : BaseFieldSpellCard
{
    private const int PrintedAtkDef = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkDef) };

    public Forest()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target)
    {
        DuelMonsterRace r = target.DuelMonsterRace;
        if (r is DuelMonsterRace.Insect or DuelMonsterRace.Beast or DuelMonsterRace.Plant or DuelMonsterRace.BeastWarrior)
            return new StatEffectTotal(
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded),
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded));
        return StatEffectTotal.None;
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, true);
    }
}
