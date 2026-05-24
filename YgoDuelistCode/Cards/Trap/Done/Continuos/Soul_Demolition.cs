using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;

/// <summary>
/// Continuous Trap: once per turn, right-click in the Spell/Trap zone to banish 1 monster you control,
/// then draw {Mgc}, gain 1 Energy, and gain 1 Conduit.
/// </summary>
public sealed class Soul_Demolition
    : BaseContinuousTrapCard, IYgoCardZoneRightClick
{
    private const string AnnualKey = "SOUL_DEMOLITION";
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    private static readonly LocString BanishControlledMonsterPrompt =
        new("cards", "YGODUELIST-SOUL_DEMOLITION.banish_controlled_monster");

    private bool _activationFlowActive;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 1m) };

    public override bool UseAlternateUpgradedDescription => true;

    public Soul_Demolition()
        : base(cost: 2, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Trap | YgoCardPackTags.Fiend | YgoCardPackTags.Dark | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[]
    {
        typeof(Soul_Demolition),
        typeof(Spell_Economics),
    };

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

        if (!HasAnyControlledFieldMonster(Owner))
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

        if (!HasAnyControlledFieldMonster(Owner))
            return null;

        return YgoNHandPlayPhaseHighlightColors.CallOfTheMummyYellow;
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);

    private static bool IsLocalControllingOwner(CardModel card)
    {
        if (card.Owner?.RunState == null)
            return false;
        Player? me = LocalContext.GetMe(card.Owner.RunState);
        return me != null && ReferenceEquals(me, card.Owner);
    }

    private static bool HasAnyControlledFieldMonster(Player player) =>
        BuildControlledFieldMonsterCandidates(player).Count > 0;

    private static List<(Creature pet, BaseMonsterCard card)> BuildControlledFieldMonsterCandidates(Player player)
    {
        var list = new List<(Creature, BaseMonsterCard)>();
        if (player.PlayerCombatState == null)
            return list;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard m)
                continue;
            list.Add((pet, m));
        }

        return list;
    }

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

            List<(Creature pet, BaseMonsterCard card)> pool = BuildControlledFieldMonsterCandidates(player);
            if (pool.Count == 0)
                return;

            var ctx = YgoChoiceContexts.Blocking();

            BaseMonsterCard? picked = await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                new CardSelectorPrefs(BanishControlledMonsterPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => pool.Select(t => t.card).ToList());

            if (picked == null)
                return;

            (Creature pet, BaseMonsterCard card)? chosen = pool.FirstOrDefault(t => ReferenceEquals(t.card, picked));
            if (chosen == null)
                return;

            if (!YgoAnnualTracker.TryConsumeAnnual(player, AnnualKey))
                return;

            await DuelMonsterPetDeathPatch.ReleaseLiveFieldMonsterToBanishedAsync(
                player,
                chosen.Value.pet,
                chosen.Value.card);

            int drawCount = (int)DynamicVars["Mgc"].BaseValue;
            if (drawCount > 0)
                await CardPileCmd.Draw(ctx, drawCount, player);

            await PlayerCmd.GainEnergy(1, player);
            await PlayerCmd.GainStars(1, player);
        }
        finally
        {
            _activationFlowActive = false;
            SpellTrapCardRightClickPatch.RefreshHolder(holder);
        }
    }
}
