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

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;

public sealed class Necrovalley : BaseFieldSpellCard
{
    private const int PrintedAtkDef = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkDef) };

    public Necrovalley()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Earth | YgoCardPackTags.Dark;

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target) =>
        IsGravekeepersMonster(target)
            ? new StatEffectTotal(
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded),
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded))
            : StatEffectTotal.None;

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, true);
    }

    private static bool IsGravekeepersMonster(BaseMonsterCard m) =>
        m.Id.Entry.Contains("GRAVEKEEPER", StringComparison.OrdinalIgnoreCase);
}
