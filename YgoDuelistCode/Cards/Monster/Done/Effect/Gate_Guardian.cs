using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Gate_Guardian : EffectMonsterCard, IYgoNamedTripleTributeSummon
{
    public override int AttackPortionCount => 3;
    /// <summary>Printed DEF added by summon effect; reapplied after full save load (see <c>CardModelFromSerializableMonsterPermanentStatsPatch</c>).</summary>
    [SavedProperty]
    public int GateGuardianSummonPrintedDefBonus { get; set; }

    public Gate_Guardian()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 11,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 37,
            baseDef: 34,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Warrior,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Warrior | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Kazejin), typeof(Suijin), typeof(Sanga_of_the_Thunder) };

    public override bool SpecialSummonGrantsImmediateCommandsThisTurn => true;

    public override bool AllowsMausoleumHpTributeForThisTributeSummon => false;

    protected override int? TributeReleaseCountOverride => 3;

    public bool CanMeetNamedTripleTributeRequirement(Player? player) => CanMeetNamedTributeRequirement(player);

    public bool NamedTributeRecipeMatches(List<Creature>? pets, int mausoleumHpTributes, int mausoleumHpLossTotal)
    {
        if (mausoleumHpTributes != 0 || mausoleumHpLossTotal != 0)
            return false;
        return TributeSelectionMeetsGateGuardianRecipe(pets);
    }

    public override int MinTributeSelectionPickCount(int tributeNeed) => tributeNeed;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        base.CanonicalKeywords.Concat(
            IsUpgradedOrPreviewActive
                ? new[] { CardKeyword.Retain }
                : Enumerable.Empty<CardKeyword>());

    public static bool CanMeetNamedTributeRequirement(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;

        List<BaseMonsterCard> field = TributeSummonSelection.BuildTributeCandidateCards(player);
        bool hasK = field.Any(c => c is Kazejin);
        bool hasSui = field.Any(c => c is Suijin);
        bool hasSanga = field.Any(c => c is Sanga_of_the_Thunder);
        if (!hasK || !hasSui || !hasSanga)
            return false;

        return DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 3);
    }

    /// <summary>
    /// Exactly three field releases: one <see cref="Kazejin"/>, one <see cref="Suijin"/>, one <see cref="Sanga_of_the_Thunder"/>.
    /// The tribute pipeline rejects Mausoleum HP rows before this is called.
    /// </summary>
    public static bool TributeSelectionMeetsGateGuardianRecipe(List<Creature>? pets)
    {
        if (pets == null || pets.Count != 3)
            return false;

        var cards = new List<BaseMonsterCard>(3);
        foreach (Creature pet in pets)
        {
            BaseMonsterCard? c = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
            if (c == null)
                return false;
            cards.Add(c);
        }

        if (cards.Distinct().Count() != 3)
            return false;

        return cards.Count(c => c is Kazejin) == 1
            && cards.Count(c => c is Suijin) == 1
            && cards.Count(c => c is Sanga_of_the_Thunder) == 1;
    }

    public override void FilterTributeSelectionGridCandidates(Player player, List<CardModel> candidates, int need)
    {
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            CardModel c = candidates[i];
            if (c is IYgoMausoleumHpTributeOption)
            {
                candidates.RemoveAt(i);
                continue;
            }

            if (c is BaseMonsterCard bm && bm is not Kazejin and not Suijin and not Sanga_of_the_Thunder)
                candidates.RemoveAt(i);
        }
    }

    protected override void OnBeforeDuelMonsterSummon(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        TributeSummonPendingResolution? tributePending)
    {
        int flat = (int)DynamicVars["Mgc"].BaseValue;
        if (flat <= 0)
            return;

        ApplyPermanentExecuteAtkDelta(flat);
        ApplyPermanentGateGuardianSummonDefDelta(flat);
    }

    private void ApplyPermanentGateGuardianSummonDefDelta(int delta)
    {
        if (delta == 0)
            return;
        AssertMutable();
        GateGuardianSummonPrintedDefBonus += delta;
        if (DynamicVars != null)
        {
            DynamicVars["Def"].BaseValue += delta;
            if (DynamicVars.Block != null)
                DynamicVars.Block.BaseValue += delta;
        }

        if (DeckVersion is Gate_Guardian deck && !ReferenceEquals(deck, this))
        {
            deck.GateGuardianSummonPrintedDefBonus += delta;
            if (deck.DynamicVars != null)
            {
                deck.DynamicVars["Def"].BaseValue += delta;
                if (deck.DynamicVars.Block != null)
                    deck.DynamicVars.Block.BaseValue += delta;
            }
        }
    }

    public override void ApplyPostDeserializePrintedStatBonuses()
    {
        base.ApplyPostDeserializePrintedStatBonuses();
        ApplySavedGateGuardianSummonDefBonusToPrintedDefense();
    }

    internal void ApplySavedGateGuardianSummonDefBonusToPrintedDefense()
    {
        if (GateGuardianSummonPrintedDefBonus == 0 || DynamicVars == null)
            return;
        CardModel template = ModelDb.GetById<CardModel>(Id).ToMutable();
        for (int i = 0; i < CurrentUpgradeLevel; i++)
        {
            template.UpgradeInternal();
            template.FinalizeUpgradeInternal();
        }

        decimal baselineDef = template.DynamicVars["Def"].BaseValue;
        DynamicVars["Def"].BaseValue = baselineDef + GateGuardianSummonPrintedDefBonus;
        if (DynamicVars.Block != null && template.DynamicVars.Block != null)
            DynamicVars.Block.BaseValue = template.DynamicVars.Block.BaseValue + GateGuardianSummonPrintedDefBonus;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        ApplySavedGateGuardianSummonDefBonusToPrintedDefense();
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
    }
}
