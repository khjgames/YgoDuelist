using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using MegaCrit.Sts2.Core.Commands;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Reckless_Greed : BaseTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar("Mgc", 2m),
            new DynamicVar("Mgc2", 2m),
        };

    public Reckless_Greed()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Draw "Mgc" cards
        await CardPileCmd.Draw(choiceContext, DynamicVars["Mgc"].BaseValue, Owner);
        // Draw 1 less card for the next "Mgc2" turns
		await PowerCmd.Apply<Draw1LessPerTurnPower>(base.Owner.Creature, DynamicVars["Mgc2"].BaseValue, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(1m);
        DynamicVars["Mgc2"].UpgradeValueBy(-1m);
        EnergyCost.UpgradeBy(-1);
    }
}
