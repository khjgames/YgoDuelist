using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Fairy_Box : BaseContinuousTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m), new DynamicVar("Mgc2", 5m) };

    public Fairy_Box()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Burn | YgoCardPackTags.Chance;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Fairy_Box),
    };

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
