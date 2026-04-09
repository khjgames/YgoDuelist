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

public sealed class Gaia_Power : BaseFieldSpellCard
{
    private const int PrintedAtk = 5;
    private const int PrintedDefPenalty = -4;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", PrintedAtk), new DynamicVar("Mgc2", 4m) };

    public Gaia_Power()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Earth;

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target) =>
        target.DuelMonsterAttribute == DuelMonsterAttribute.Earth
            ? new StatEffectTotal(
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtk, IsUpgraded),
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedDefPenalty, IsUpgraded))
            : StatEffectTotal.None;

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtk, true);
        DynamicVars["Mgc2"].BaseValue = System.Math.Abs(
            YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedDefPenalty, true));
    }
}
