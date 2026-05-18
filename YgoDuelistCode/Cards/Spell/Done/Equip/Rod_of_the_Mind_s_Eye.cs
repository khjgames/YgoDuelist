using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Rod_of_the_Mind_s_Eye : BaseEquipSpellCard
{
    private decimal BlightPercent => IsUpgraded ? 50m : 35m;

    public Rod_of_the_Mind_s_Eye()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Spellcaster;

    public override bool UseAlternateUpgradedDescription => true;

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

    public override bool CanEquipTo(BaseMonsterCard target) => target.DuelMonsterRace == DuelMonsterRace.Spellcaster;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

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
