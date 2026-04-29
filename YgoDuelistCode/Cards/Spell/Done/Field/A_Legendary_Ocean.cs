using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;

public sealed class A_Legendary_Ocean : BaseFieldSpellCard
{
    private const int PrintedAtkDef = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkDef), new DynamicVar("Mgc2", 1m) };
        
    public A_Legendary_Ocean()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Water | YgoCardPackTags.Ocean;

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target)
    {
        if (target.DuelMonsterAttribute != DuelMonsterAttribute.Water)
            return StatEffectTotal.None;
        int atk = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded);
        int def = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded);
        return new StatEffectTotal(atk, def, -(int)DynamicVars["Mgc2"].BaseValue);
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, true);
        DynamicVars["Mgc2"].BaseValue = 2m;
    }
}
