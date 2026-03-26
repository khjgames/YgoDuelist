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

public sealed class A_Legendary_Ocean : BaseFieldSpellCard
{
    private const int PrintedAtkDef = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkDef) };

    public A_Legendary_Ocean()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target)
    {
        if (target.DuelMonsterAttribute != DuelMonsterAttribute.Water)
            return StatEffectTotal.None;
        int atk = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded);
        int def = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded);
        return new StatEffectTotal(atk, def, -1);
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, true);
    }
}
