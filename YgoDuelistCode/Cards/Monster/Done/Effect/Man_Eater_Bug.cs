using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Man_Eater_Bug : EffectMonsterCard, IMonsterFlipEffect
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";
    private static readonly LocString FlipPrompt = new("cards", "YGODUELIST-MAN_EATER_BUG.flip_destroy_select");

    public Man_Eater_Bug()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 4,
            baseDef: 6,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override bool UseAlternateUpgradedDescription => true;

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Insect;

    public override Type[] RelatedCards => new[] { typeof(Man_Eater_Bug) };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new EnergyVar(0) }.Concat(base.CanonicalVars);

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Man_Eater_Bug || Owner == null)
            return;

        await YgoFlipReturnOwnFieldMonsterToHand.RunFlipDestroyOneOtherControlledWithConduitAsync(
            choiceContext,
            Owner,
            self,
            FlipPrompt,
            IsUpgraded);
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
