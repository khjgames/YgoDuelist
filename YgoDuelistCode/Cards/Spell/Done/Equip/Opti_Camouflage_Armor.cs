using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Opti_Camouflage_Armor : BaseEquipSpellCard
{
    private const decimal BlightPercent = 100m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

    public Opti_Camouflage_Armor()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override bool CardShowsBlightKeyword => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    public override bool CanEquipTo(BaseMonsterCard target) =>
        target.GetEffectiveDuelMonsterLevel() <= (int)DynamicVars["Mgc"].BaseValue;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 2m;

    protected internal override void OnAfterAttachedToFieldMonster(BaseMonsterCard equippedMonster)
    {
        Player? player = equippedMonster.Owner ?? Owner;
        if (player == null)
            return;

        MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely(
            YgoEquipPetPowerAttach.ApplyBlightPercentAsync(player, equippedMonster, BlightPercent, this));
    }

    protected internal override void OnAfterDetachedFromFieldMonster(BaseMonsterCard equippedMonster)
    {
        MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely(
            YgoEquipPetPowerAttach.RemoveBlightPercentAsync(equippedMonster));
    }
}
