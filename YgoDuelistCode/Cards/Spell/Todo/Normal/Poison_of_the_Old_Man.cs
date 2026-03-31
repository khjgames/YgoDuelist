using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Poison_of_the_Old_Man : BaseSpellCard
{
    private const int OptionHealSelf = 0;
    private const int OptionBlightEnemy = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 3m), new DynamicVar("Mgc2", 12m) };

    public Poison_of_the_Old_Man()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.AnyEnemy, duelMonsterRace: YgoDuelist.YgoDuelistCode.Models.DuelMonsterRace.SpellQuickPlay)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Heal | YgoCardPackTags.Spell;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Poison_of_the_Old_Man),
    };

    public override bool CardShowsBlightKeyword => true;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState is not { } cs)
            return;

        Player player = Owner;

        YgoTransientSpellOptionCommandCard healOpt = YgoTransientSpellOptionCommandCard.Create(
            cs,
            player,
            OptionHealSelf,
            "YGODUELIST-POISON_OF_THE_OLD_MAN_OPT_HEAL.title",
            "YGODUELIST-POISON_OF_THE_OLD_MAN_OPT_HEAL.description",
            this,
            "poison_of_the_old_man.png");

        YgoTransientSpellOptionCommandCard blightOpt = YgoTransientSpellOptionCommandCard.Create(
            cs,
            player,
            OptionBlightEnemy,
            "YGODUELIST-POISON_OF_THE_OLD_MAN_OPT_BLIGHT.title",
            "YGODUELIST-POISON_OF_THE_OLD_MAN_OPT_BLIGHT.description",
            this,
            "poison_of_the_old_man.png");

        var options = new List<CardModel> { healOpt, blightOpt };

        CardModel? pick = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            options,
            player,
            canSkip: false);

        if (pick is not YgoTransientSpellOptionCommandCard chosen)
            return;

        if (chosen.OptionId == OptionHealSelf)
        {
            await CreatureCmd.Heal(Owner.Creature, DynamicVars["Mgc"].BaseValue);
            return;
        }

        if (chosen.OptionId == OptionBlightEnemy)
        {
            if (cardPlay.Target == null)
                return;
            await PowerCmd.Apply<BlightPower>(
                cardPlay.Target,
                DynamicVars["Mgc2"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(2m);
        DynamicVars["Mgc2"].UpgradeValueBy(8m);
    }
}
