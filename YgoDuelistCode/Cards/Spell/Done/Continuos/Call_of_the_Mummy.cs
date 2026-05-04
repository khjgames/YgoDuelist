using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
/// Continuous Spell: once per turn, right-click in the Spell/Trap zone while you control no monsters to Special Summon
/// 1 Zombie from the hand (yellow highlight when legal).
/// </summary>
public sealed class Call_of_the_Mummy
    : BaseContinuousSpellCard, IYgoCardZoneRightClick
{
    private const string AnnualKey = "CALL_OF_THE_MUMMY";

    private static readonly LocString SelectionPrompt =
        new("cards", "YGODUELIST-CALL_OF_THE_MUMMY.selectionScreenPrompt");

    private bool _summonFlowActive;

    public Call_of_the_Mummy()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Zombie;

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    public YgoCardRightClickActivation RightClickActivationMask =>
        YgoCardRightClickActivation.SpellTrapZoneFaceUp;

    public bool TryHandleCardZoneRightClick(NHandCardHolder holder)
    {
        if (!IsLocalControllingOwner(this))
            return false;

        if (_summonFlowActive)
            return true;

        if (Owner == null || Pile?.Type != SpellTrapZonePile.CustomType || FaceDown)
            return false;

        if (!YgoAnnualTracker.IsAnnualAvailable(Owner, AnnualKey))
            return false;

        if (!HasAnyEligibleZombieInHand(Owner))
            return false;

        _ = RunSummonFlowAsync(holder);
        return true;
    }

    public override Color? GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight)
    {
        _ = holder;
        PileType pileType = Pile?.Type ?? PileType.None;
        if (pileType != SpellTrapZonePile.CustomType || FaceDown)
            return null;
        if (Owner == null)
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

        if (!HasAnyEligibleZombieInHand(Owner))
            return null;

        bool faceUpInZone = true;
        if (!vanillaWouldUseCyanPlayableHighlight && !faceUpInZone)
            return null;

        return YgoNHandPlayPhaseHighlightColors.CallOfTheMummyYellow;
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static bool IsLocalControllingOwner(CardModel card)
    {
        if (card.Owner?.RunState == null)
            return false;
        Player? me = LocalContext.GetMe(card.Owner.RunState);
        return me != null && ReferenceEquals(me, card.Owner);
    }

    private static bool HasAnyEligibleZombieInHand(Player player) =>
        BuildEligibleHandZombies(player).Count > 0;

    private static List<BaseMonsterCard> BuildEligibleHandZombies(Player player) =>
        TributeSummonGridSelect
            .BuildStabilizedHandCandidates(
                player,
                c => c is BaseMonsterCard bm && IsEligibleZombieHandSpecial(player, bm),
                null)
            .OfType<BaseMonsterCard>()
            .ToList();

    /// <summary>
    /// Mirrors <see cref="DuelMonsterSummon.TrySummonDuelMonster"/> special-summon gates plus this card's Zombie / empty-field rules.
    /// </summary>
    private static bool IsEligibleZombieHandSpecial(Player player, BaseMonsterCard card)
    {
        if (card.DuelMonsterRace != DuelMonsterRace.Zombie)
            return false;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null || card.Pile != hand)
            return false;

        if (DuelMonsterSummon.CountLiveDuelMonsters(player) != 0)
            return false;

        if (!card.CanSummonDuelMonster && !card.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate)
            return false;

        if (card.BlocksSpecialDuelMonsterSummon)
            return false;

        if (player.Creature == null || player.PlayerCombatState == null)
            return false;

        if (!HasResolvableCombatStateForSummon(player))
            return false;

        if (!ReactorSlimeSummonGate.AllowsSummon(player, card))
            return false;

        if (!YgoFushiohRichieSummonGate.AllowsSpecialSummon(player, card))
            return false;

        return DuelMonsterSummon.CountLiveDuelMonsters(player) < DuelMonsterSummon.MaxDuelMonstersPerPlayer;
    }

    private static bool HasResolvableCombatStateForSummon(Player player)
    {
        if (player.Creature?.CombatState != null)
            return true;

        if (player.PlayerCombatState != null)
        {
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (pet?.CombatState != null)
                    return true;
            }
        }

        return CombatManager.Instance?.DebugOnlyGetState() != null;
    }

    private async Task RunSummonFlowAsync(NHandCardHolder holder)
    {
        _summonFlowActive = true;
        try
        {
            Player? player = Owner;
            if (player == null)
                return;

            if (!YgoAnnualTracker.IsAnnualAvailable(player, AnnualKey))
                return;

            List<BaseMonsterCard> pool = BuildEligibleHandZombies(player);
            if (pool.Count == 0)
                return;

            var ctx = YgoChoiceContexts.Blocking();

            BaseMonsterCard? picked = await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                new CardSelectorPrefs(SelectionPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildEligibleHandZombies(player));

            if (picked == null || !pool.Contains(picked))
                return;

            if (!IsEligibleZombieHandSpecial(player, picked))
                return;

            if (!YgoAnnualTracker.TryConsumeAnnual(player, AnnualKey))
                return;

            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, picked, ctx);
        }
        finally
        {
            _summonFlowActive = false;
            SpellTrapCardRightClickPatch.RefreshHolder(holder);
        }
    }
}
