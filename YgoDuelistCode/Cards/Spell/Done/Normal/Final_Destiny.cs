using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Final_Destiny : BaseSpellCard
{
    private const int HandCardsToDestroy = 5;

    private static readonly LocString HandDestroySelectionPrompt =
        new("cards", "YGODUELIST-FINAL_DESTINY.hand_selection");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 18m) };

    public Final_Destiny()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Draw | YgoCardPackTags.Burn;

    public override bool CardShowsBlightKeyword => true;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && YgoPlayerPiles.Hand(Owner) is { } hand
        && hand.Cards.Count >= HandCardsToDestroy + (Pile?.Type == PileType.Hand ? 1 : 0);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState is not CombatState cs)
            return;

        List<CardModel> picked = (await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(HandDestroySelectionPrompt, HandCardsToDestroy, HandCardsToDestroy)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildHandDestroyCandidates(Owner, this),
            HandCardsToDestroy,
            PlayerChoiceOptions.CancelPlayCardActions)).Cast<CardModel>().ToList();

        if (picked.Count < HandCardsToDestroy)
            return;

        CardPile? gy = YgoPlayerPiles.Graveyard(Owner);
        if (gy == null)
            return;

        foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(picked))
        {
            await CardPileCmd.Add(
                new[] { c },
                gy,
                CardPilePosition.Top,
                c,
                false);
        }

        decimal blight = DynamicVars["Mgc"].BaseValue;
        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
            await PowerCmd.Apply<BlightPower>(enemy, blight, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(12m);

    private static List<CardModel> BuildHandDestroyCandidates(Player player, CardModel sourceCard) =>
        TributeSummonGridSelect.BuildStabilizedHandCandidates(player, null, sourceCard);
}
