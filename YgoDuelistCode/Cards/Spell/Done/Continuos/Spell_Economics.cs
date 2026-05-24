using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;

/// <summary>
/// Continuous Spell (Soul Economics): once per turn, right-click in the Spell/Trap zone to banish
/// 1 monster from your Graveyard and gain 1 Conduit. Upgraded: Innate.
/// </summary>
public sealed class Spell_Economics
    : BaseContinuousSpellCard, IYgoCardZoneRightClick
{
    private const string AnnualKey = "SPELL_ECONOMICS";
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    private static readonly LocString BanishPrompt =
        new("cards", "YGODUELIST-SPELL_ECONOMICS.banish_graveyard");

    private bool _activationFlowActive;

    public Spell_Economics()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Banish;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Concat(IsUpgraded ? new[] { CardKeyword.Innate } : Enumerable.Empty<CardKeyword>());

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    public YgoCardRightClickActivation RightClickActivationMask =>
        YgoCardRightClickActivation.SpellTrapZoneFaceUp;

    public bool TryHandleCardZoneRightClick(NHandCardHolder holder)
    {
        if (!IsLocalControllingOwner(this))
            return false;

        if (_activationFlowActive)
            return true;

        if (Owner == null || Pile?.Type != SpellTrapZonePile.CustomType || FaceDown)
            return false;

        if (!YgoAnnualTracker.IsAnnualAvailable(Owner, AnnualKey))
            return false;

        if (!HasAnyMonsterInGraveyard(Owner))
            return false;

        _ = RunBanishFlowAsync(holder);
        return true;
    }

    public override Color? GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight)
    {
        _ = holder;
        if (Pile?.Type != SpellTrapZonePile.CustomType || FaceDown || Owner == null)
            return null;

        try
        {
            if (!LocalContext.IsMe(Owner))
                return null;
        }
        catch
        {
            return null;
        }

        if (!YgoAnnualTracker.IsAnnualAvailable(Owner, AnnualKey))
            return null;

        if (!HasAnyMonsterInGraveyard(Owner))
            return null;

        return YgoNHandPlayPhaseHighlightColors.CallOfTheMummyYellow;
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        if (!Keywords.Contains(CardKeyword.Innate))
            AddKeyword(CardKeyword.Innate);
    }

    private static bool IsLocalControllingOwner(CardModel card)
    {
        if (card.Owner?.RunState == null)
            return false;
        Player? me = LocalContext.GetMe(card.Owner.RunState);
        return me != null && ReferenceEquals(me, card.Owner);
    }

    private static bool HasAnyMonsterInGraveyard(Player player) =>
        BuildGraveyardMonsterCandidates(player).Count > 0;

    private static List<BaseMonsterCard> BuildGraveyardMonsterCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .ToList();

    private async Task RunBanishFlowAsync(NHandCardHolder holder)
    {
        _activationFlowActive = true;
        try
        {
            Player? player = Owner;
            if (player == null)
                return;

            if (!YgoAnnualTracker.IsAnnualAvailable(player, AnnualKey))
                return;

            List<BaseMonsterCard> pool = BuildGraveyardMonsterCandidates(player);
            if (pool.Count == 0)
                return;

            var ctx = YgoChoiceContexts.Blocking();

            BaseMonsterCard? picked = await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                new CardSelectorPrefs(BanishPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildGraveyardMonsterCandidates(player));

            if (picked == null || !pool.Contains(picked))
                return;

            if (!YgoPlayerPiles.GraveyardContains(player, picked))
                return;

            if (!YgoAnnualTracker.TryConsumeAnnual(player, AnnualKey))
                return;

            await YgoBanishedService.BanishCard(player, picked);
            await PlayerCmd.GainStars(1, player);
        }
        finally
        {
            _activationFlowActive = false;
            SpellTrapCardRightClickPatch.RefreshHolder(holder);
        }
    }
}
