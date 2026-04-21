using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Super_Rejuvenation : BaseSpellCard
{
    public override bool UseAlternateUpgradedDescription => true;

    private bool ShowUpgradedSuperRejuvenationPowerHover =>
        IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None;

    public Super_Rejuvenation()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Spell | YgoCardPackTags.Draw;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            var preview = (SuperRejuvenationPower)ModelDb.Power<SuperRejuvenationPower>().ToMutable(1);
            preview.SetCardTooltipUpgradePreview(ShowUpgradedSuperRejuvenationPowerHover);
            yield return preview.DumbHoverTip;
        }
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        // Amount 1 is a placeholder so Apply runs; real draw count is set at end of this turn on the power.
        await PowerCmd.Apply<SuperRejuvenationPower>(Owner.Creature, 1, Owner.Creature, this);
    }
}
