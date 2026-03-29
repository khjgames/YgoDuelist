using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Reload : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 0m) };

    public override bool UseAlternateUpgradedDescription => true;

    public Reload()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> cards = PileType.Hand.GetPile(base.Owner).Cards;
        int cardsToDraw = cards.Count() + (int)DynamicVars["Mgc"].BaseValue;
        await CardCmd.DiscardAndDraw(choiceContext, cards, cardsToDraw);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(1m);
    }
}
