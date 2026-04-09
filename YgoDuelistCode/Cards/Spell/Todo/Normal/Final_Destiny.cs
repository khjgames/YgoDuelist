using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Final_Destiny : BaseSpellCard
{
    private const int HandCardsToDestroy = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 18m) };

    public Final_Destiny()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw | YgoCardPackTags.Burn;

    public override bool CardShowsBlightKeyword => true;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && PileType.Hand.GetPile(Owner)?.Cards.Count >= HandCardsToDestroy + 1;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState is not CombatState cs)
            return;

        var picked = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, HandCardsToDestroy, HandCardsToDestroy)
            {
                RequireManualConfirmation = true,
                Cancelable = false
            },
            c => !ReferenceEquals(c, this),
            this)).ToList();

        if (picked.Count < HandCardsToDestroy)
            return;

        CardPile? gy = GraveyardPile.CustomType.GetPile(Owner);
        if (gy == null)
            return;

        foreach (CardModel c in picked)
        {
            await CardPileCmd.Add(
                new[] { c },
                gy,
                CardPilePosition.Top,
                c,
                false);
        }

        decimal blight = DynamicVars["Mgc"].BaseValue;
        foreach (Creature enemy in cs.HittableEnemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<BlightPower>(enemy, blight, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(12m);
}
