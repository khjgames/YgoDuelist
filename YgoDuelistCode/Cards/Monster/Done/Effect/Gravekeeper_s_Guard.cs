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

public sealed class Gravekeeper_s_Guard : EffectMonsterCard, IMonsterFlipEffect
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";
    private static readonly LocString FlipPrompt = new("cards", "YGODUELIST-GRAVEKEEPER_S_GUARD.flip_return_select");

    public Gravekeeper_s_Guard()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 10,
            baseDef: 19,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override bool UseAlternateUpgradedDescription => true;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Spell | YgoCardPackTags.Earth;

    /// <summary>Do not include self — duplicate model id breaks MP pool registration.</summary>
    public override Type[] RelatedCards => System.Array.Empty<Type>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new EnergyVar(0) }.Concat(base.CanonicalVars);

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Gravekeeper_s_Guard || Owner == null)
            return;

        await YgoFlipReturnOwnFieldMonsterToHand.RunFlipReturnOneOtherControlledWithConduitAsync(
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
