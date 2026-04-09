using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Talisman_of_Trap_Sealing : BaseContinuousSpellCard
{
    private const string AnnualKey = "TALISMAN_TRAP_SEALING";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 2m) };

    public Talisman_of_Trap_Sealing()
        : base(0, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Trap;

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override bool IsPlayable
    {
        get
        {
            bool inZoneFaceUp = Pile?.Type == SpellTrapZonePile.CustomType && !FaceDown;
            if (!inZoneFaceUp && !base.IsPlayable)
                return false;
            if (!YgoSealmasterMeiseiGate.HasFaceUpSealmaster(Owner))
                return false;
            if (Pile?.Type != SpellTrapZonePile.CustomType || FaceDown || Owner == null)
                return true;
            if (!YgoAnnualTracker.IsAnnualAvailable(Owner, AnnualKey))
                return false;
            CardPile? hand = PileType.Hand.GetPile(Owner);
            return hand?.Cards.Any(IsStatusOrCurse) == true;
        }
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        // Initial activation from hand/set just moves this card face-up into the zone.
        if (Pile?.Type != SpellTrapZonePile.CustomType || FaceDown)
            return;

        if (!YgoSealmasterMeiseiGate.HasFaceUpSealmaster(Owner))
            return;
        if (!YgoAnnualTracker.TryConsumeAnnual(Owner, AnnualKey))
            return;

        await ExhaustStatusesOrCursesAsync(choiceContext, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);

    private async Task ExhaustStatusesOrCursesAsync(PlayerChoiceContext choiceContext, Player player)
    {
        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null || hand.Cards.Count == 0)
            return;

        int eligible = hand.Cards.Count(IsStatusOrCurse);
        if (eligible == 0)
            return;

        int maxPick = Math.Min((int)DynamicVars["Mgc"].BaseValue, eligible);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, maxPick)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        var pick = await CardSelectCmd.FromHand(choiceContext, player, prefs, IsStatusOrCurse, this);
        foreach (CardModel c in pick.ToList())
            await CardCmd.Exhaust(choiceContext, c);
    }

    private static bool IsStatusOrCurse(CardModel c) =>
        c.Type == CardType.Status || c.Type == CardType.Curse;
}
