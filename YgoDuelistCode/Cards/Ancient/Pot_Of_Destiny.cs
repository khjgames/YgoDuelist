using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell;

public sealed class Pot_Of_Destiny : BaseSpellCard
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new EnergyVar(1), new StarsVar(1), new CardsVar(2) };

    public override string CustomPortraitPath => ModelDb.Card<Pot_Of_Greed>().CustomPortraitPath;
    public override string PortraitPath => ModelDb.Card<Pot_Of_Greed>().PortraitPath;
    public override string BetaPortraitPath => ModelDb.Card<Pot_Of_Greed>().BetaPortraitPath;

    public Pot_Of_Destiny()
        : base(0, CardRarity.Ancient, TargetType.Self, DuelMonsterRace.SpellNormal)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Pot_Of_Greed),
    };

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await PlayerCmd.GainStars(DynamicVars.Stars.IntValue, Owner);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("conduitIcon", ConduitImgBbcode);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
