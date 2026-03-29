using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Sparks : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 2m) };

    public Sparks()
        : base(cost: 0, cardType: CardType.Attack, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Burn | YgoCardPackTags.Spell | YgoCardPackTags.Fire | YgoCardPackTags.Starter;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Sparks),
        typeof(Hinotama),
    };

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<BlightPower>(cardPlay.Target, DynamicVars["Mgc"].BaseValue, base.Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(4m);
}
