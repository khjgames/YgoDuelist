using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;

public sealed class Fairy_Box : BaseContinuousTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m), new DynamicVar("Mgc2", 5m) };

    public Fairy_Box()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override bool MatchesFairyBoxFieldPowerTier(bool expectPlus) => !FaceDown && IsUpgraded == expectPlus;

    private bool ShowFairyBoxPlusPowerHover => IsUpgradedOrPreviewActive;
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Chance | YgoCardPackTags.Trap | YgoCardPackTags.Bundled;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    public override Type[] BundledCards => new[] { typeof(Fairy_Box) };

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Fairy_Box),
        typeof(Heads),
        typeof(Tails)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            if (ShowFairyBoxPlusPowerHover)
                yield return HoverTipFactory.FromPower<FairyBoxFieldPowerPlus>();
            else
                yield return HoverTipFactory.FromPower<FairyBoxFieldPower>();
        }
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature? creature = Owner?.Creature;
        if (creature == null)
            return;

        await PowerCmd.Remove<FairyBoxFieldPower>(creature);
        await PowerCmd.Remove<FairyBoxFieldPowerPlus>(creature);

        if (IsUpgraded)
            await PowerCmd.Apply<FairyBoxFieldPowerPlus>(creature, 1m, creature, this);
        else
            await PowerCmd.Apply<FairyBoxFieldPower>(creature, 1m, creature, this);

        if (creature.CombatState is { } cs)
            await YgoFairyBoxHeadsTailsWeak.RunImmediateActivationAsync(choiceContext, cs, Owner!, creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(1m);
        DynamicVars["Mgc2"].UpgradeValueBy(-2m);
    }
}
