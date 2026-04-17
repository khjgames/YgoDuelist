using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;

public sealed class Talisman_of_Trap_Sealing
    : BaseContinuousSpellCard, IYgoCardZoneRightClick, IYgoAfterDuelMonsterDiedZoneCard, IYgoSealmasterDependentTalisman
{
    private const string AnnualKey = "TALISMAN_TRAP_SEALING";

    private bool _activationFlowActive;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 2m) };

    public Talisman_of_Trap_Sealing()
        : base(0, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    public YgoCardRightClickActivation RightClickActivationMask =>
        YgoCardRightClickActivation.SpellTrapZoneFaceUp;

    public Task AfterDuelMonsterDiedAsync(DuelMonsterPetDeathContext ctx) =>
        YgoSealmasterMeiseiGate.DestroyTalismansIfNoSealmaster(ctx.Player);

    public bool TryHandleCardZoneRightClick(NHandCardHolder holder)
    {
        if (!IsLocalControllingOwner(this))
            return false;

        if (_activationFlowActive)
            return true;

        if (Owner == null || Pile?.Type != SpellTrapZonePile.CustomType || FaceDown)
            return false;

        if (!YgoSealmasterMeiseiGate.HasFaceUpSealmaster(Owner))
            return false;

        if (!YgoAnnualTracker.IsAnnualAvailable(Owner, AnnualKey))
            return false;

        CardPile? hand = PileType.Hand.GetPile(Owner);
        if (hand?.Cards.Any(IsStatusOrCurse) != true)
            return false;

        _ = RunTrapSealingActivationAsync(holder);
        return true;
    }

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

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);

    private static bool IsLocalControllingOwner(CardModel card)
    {
        if (card.Owner?.RunState == null)
            return false;
        Player? me = LocalContext.GetMe(card.Owner.RunState);
        return me != null && ReferenceEquals(me, card.Owner);
    }

    private async Task RunTrapSealingActivationAsync(NHandCardHolder holder)
    {
        _activationFlowActive = true;
        try
        {
            Player? player = Owner;
            if (player == null)
                return;

            if (!YgoSealmasterMeiseiGate.HasFaceUpSealmaster(player))
                return;

            if (!YgoAnnualTracker.TryConsumeAnnual(player, AnnualKey))
                return;

            var ctx = new BlockingPlayerChoiceContext();
            await ExhaustStatusesOrCursesAsync(ctx, player);
        }
        finally
        {
            _activationFlowActive = false;
            SpellTrapCardRightClickPatch.RefreshHolder(holder);
        }
    }

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

        List<CardModel> candidates = TributeSummonGridSelect.BuildStabilizedHandCandidates(player, IsStatusOrCurse, null);

        var pick = await TributeSummonGridSelect.FromSimpleGridCombat(
            choiceContext,
            candidates,
            player,
            prefs,
            rebuildCanonicalForRemoteApply: () =>
                TributeSummonGridSelect.BuildStabilizedHandCandidates(player, IsStatusOrCurse, null),
            PlayerChoiceOptions.CancelPlayCardActions);
        foreach (CardModel c in pick.ToList())
            await CardCmd.Exhaust(choiceContext, c);
    }

    private static bool IsStatusOrCurse(CardModel c) =>
        c.Type == CardType.Status || c.Type == CardType.Curse;
}
