using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Character;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Base type for all YgoDuelist monster cards. Right-click toggles attack vs defense (Skill);
/// monsters with <see cref="SupportsHandEffectForm"/> also cycle a third Hand Effect form (still Skill type).
/// </summary>
public abstract class AbstractMonsterCard : YgoDuelistCard, IYgoCard
{
    private readonly int _handSummonFallbackEnergy;

    private enum MonsterDisplayForm
    {
        Attack,
        Defense,
        HandEffect
    }

    // We don't have real STS CardKeyword entries for Yu-Gi-Oh! attributes/races,
    // so we map our enums into "virtual" CardKeyword ids by using stable numeric values.
    // HoverTipFactory.FromKeyword then looks these up in localization table `card_keywords`.
    private const int AttributeKeywordBase = 10000;
    private const int RaceKeywordBase = 20000;

    private static CardKeyword AttributeToKeyword(DuelMonsterAttribute attribute)
        => (CardKeyword)(AttributeKeywordBase + (int)attribute);

    private static CardKeyword RaceToKeyword(DuelMonsterRace race)
        => (CardKeyword)(RaceKeywordBase + (int)race);

    private static CardKeyword SpecialSummonKeyword => (CardKeyword)20033;
    private static CardKeyword FaceDownKeyword => (CardKeyword)10012;
    private static CardKeyword TributeSummon1Keyword => (CardKeyword)20034;
    private static CardKeyword TributeSummon2Keyword => (CardKeyword)20035;
    private static CardKeyword FusionMonsterKeyword => (CardKeyword)20036;
    private static CardKeyword RitualMonsterKeyword => (CardKeyword)20037;
    private static CardKeyword HandEffectMonsterKeyword => (CardKeyword)20038;
    private static CardKeyword CycleMonsterKeyword => (CardKeyword)20039;
    private static CardKeyword FlipEffectKeyword => (CardKeyword)20041;
    private static CardKeyword RecklessBlockerKeyword => (CardKeyword)20042;

    public abstract YgoCardType YgoCardType { get; }
    public bool FaceDown { get; set; } = false;
    public bool WillSet { get; set; } = true;

    /// <summary>Attack, defense (Skill), or optional Hand Effect (Skill). Toggle via right-click in hand.</summary>
    private MonsterDisplayForm _displayForm;

    /// <summary>Monsters that expose a third right-click mode with <c>.description_hand_effect</c> locale keys.</summary>
    protected virtual bool SupportsHandEffectForm => false;

    /// <summary>True while in Hand Effect form (plays as Skill with that effect only).</summary>
    public bool IsHandEffectFormActive => SupportsHandEffectForm && _displayForm == MonsterDisplayForm.HandEffect;

    /// <summary>Attack position for field / command menu (excludes defense and hand-effect forms).</summary>
    public bool IsAttackBattlePosition => _displayForm == MonsterDisplayForm.Attack;

    public override CardType Type => _displayForm == MonsterDisplayForm.Attack ? CardType.Attack : CardType.Skill;
    public override TargetType TargetType =>
        _displayForm == MonsterDisplayForm.Attack ? TargetType.AnyEnemy : TargetType.Self;

    public new LocString Description => GetDescriptionLocString();

    /// <summary>Printed ATK/DEF ? / unknown: energy rule uses flat 1.</summary>
    public virtual bool DuelMonsterStatsAreUnknown => false;

    /// <summary>Energy for <see cref="SupportsHandEffectForm"/> hand-effect mode (third toggle).</summary>
    protected virtual int HandEffectMonsterEnergyCost => 0;

    protected override int CanonicalEnergyCost
    {
        get
        {
            if (this is BaseMonsterCard bm)
            {
                if (SupportsHandEffectForm && IsHandEffectFormActive)
                    return HandEffectMonsterEnergyCost;
                int baseCost = Type == CardType.Attack
                    ? bm.DuelMonsterAttackPlayEnergy
                    : bm.DuelMonsterDefensePlayEnergy;
                int discount = bm.GetDuelMonsterPlayEnergyDiscount();
                if (discount <= 0)
                    return baseCost;
                int discounted = baseCost - discount;
                return discounted < 0 ? 0 : discounted;
            }

            return _handSummonFallbackEnergy;
        }
    }

    protected AbstractMonsterCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
        _handSummonFallbackEnergy = cost;
        _displayForm = type == CardType.Attack ? MonsterDisplayForm.Attack : MonsterDisplayForm.Defense;
    }

    /// <summary>
    /// Sets whether this monster starts in attack position (Attack card) or defense position (Skill card).
    /// Called by derived classes once their stats (e.g. base ATK/DEF) are known.
    /// </summary>
    protected void SetDisplayAttackSkill(bool displayAsAttack)
    {
        _displayForm = displayAsAttack ? MonsterDisplayForm.Attack : MonsterDisplayForm.Defense;
        if (_displayForm == MonsterDisplayForm.Attack && FaceDown)
        {
            bool wasFaceDown = FaceDown;
            FaceDown = false;
            YgoMonsterFlipEffectRunner.ScheduleIfFlippedOnField(this, wasFaceDown, choiceContext: null);
        }
        else if (_displayForm == MonsterDisplayForm.Defense && !FaceDown && WillSet && CanUseSetVisualStateInCurrentForm())
            FaceDown = true;
        UpdateFaceDownKeywordFromBool();
        CardModelEnergyCache.Invalidate(this);
    }

    /// <summary>
    /// Sets attack vs defense position when the player uses <see cref="Command.Command_Attack"/> or <see cref="Command.Command_Defend"/>.
    /// Persists after the command resolves (same rules as <see cref="SetDisplayAttackSkill"/> for face-down).
    /// </summary>
    public void SetBattlePositionFromDuelCommand(bool attackPosition)
    {
        SetDisplayAttackSkill(attackPosition);
    }

    /// <summary>
    /// Monster command menu only: switches battle position.
    /// Defense → attack: face-up and <see cref="WillSet"/> = false (no automatic set).
    /// Attack → defense: position only; <see cref="FaceDown"/> and <see cref="WillSet"/> unchanged.
    /// </summary>
    /// <returns><c>true</c> if the monster switched from defense to attack this call.</returns>
    public bool ApplyBattlePositionChangeFromCommandMenu()
    {
        bool switchedFromDefenseToAttack = false;

        if (SupportsHandEffectForm && _displayForm == MonsterDisplayForm.HandEffect)
            _displayForm = MonsterDisplayForm.Defense;

        if (_displayForm == MonsterDisplayForm.Defense)
        {
            switchedFromDefenseToAttack = true;
            _displayForm = MonsterDisplayForm.Attack;
            bool wasFaceDown = FaceDown;
            FaceDown = false;
            WillSet = false;
            YgoMonsterFlipEffectRunner.ScheduleIfFlippedOnField(this, wasFaceDown, choiceContext: null);
        }
        else
        {
            _displayForm = MonsterDisplayForm.Defense;
        }

        UpdateFaceDownKeywordFromBool();
        AfterDisplayFormChanged();
        return switchedFromDefenseToAttack;
    }

    /// <summary>
    /// After this card's field monster switches from defense to attack via <see cref="ApplyBattlePositionChangeFromCommandMenu"/>.
    /// </summary>
    public virtual Task OnSwitchedFromDefenseToAttackFromCommandAsync(PlayerChoiceContext choiceContext, Player player) =>
        Task.CompletedTask;

    /// <summary>
    /// After this card's field monster switches from attack to defense via <see cref="ApplyBattlePositionChangeFromCommandMenu"/>.
    /// </summary>
    public virtual Task OnSwitchedFromAttackToDefenseFromCommandAsync(PlayerChoiceContext choiceContext, Player player) =>
        Task.CompletedTask;

    /// <summary>Cycles attack / defense, or attack / defense / hand effect when supported. Right-click in hand.</summary>
    public void ToggleAttackSkill()
    {
        if (this is BaseMonsterCard bm && MonsterCommandRegistry.SourceMonsterHasDieForYouForcedActive(Owner, bm))
            return;

        bool twoModeOnly = !SupportsHandEffectForm || YgoMonsterFormPreviewContext.RestrictMonsterToggleToAttackDefenseOnly;
        if (twoModeOnly)
        {
            _displayForm = _displayForm == MonsterDisplayForm.Attack
                ? MonsterDisplayForm.Defense
                : MonsterDisplayForm.Attack;
        }
        else
        {
            _displayForm = _displayForm switch
            {
                MonsterDisplayForm.Attack => MonsterDisplayForm.Defense,
                MonsterDisplayForm.Defense => MonsterDisplayForm.HandEffect,
                MonsterDisplayForm.HandEffect => MonsterDisplayForm.Attack,
                _ => MonsterDisplayForm.Attack
            };
        }

        if (_displayForm == MonsterDisplayForm.Attack && FaceDown)
        {
            bool wasFaceDown = FaceDown;
            FaceDown = false;
            YgoMonsterFlipEffectRunner.ScheduleIfFlippedOnField(this, wasFaceDown, choiceContext: null);
        }
        else if (_displayForm == MonsterDisplayForm.Defense && !FaceDown && WillSet && CanUseSetVisualStateInCurrentForm())
            FaceDown = true;
        else if (_displayForm == MonsterDisplayForm.HandEffect)
        {
            // Hand-effect mode should always render as face-up skill visuals.
            FaceDown = false;
        }

        UpdateFaceDownKeywordFromBool();
        AfterDisplayFormChanged();
    }

    /// <summary>Called after attack/defense/hand-effect display mode changes (toggle or command menu).</summary>
    protected virtual void AfterDisplayFormChanged()
    {
        CardModelEnergyCache.Invalidate(this);
    }

    /// <summary>Conduit star cost via StarsVar on monster cards. Fusion and ritual overrides use 0.</summary>
    protected virtual int MonsterConduitStarCost => 1;

    /// <summary>Level (star count) 1-9+ for summon HP. Override per card.</summary>
    public virtual int DuelMonsterLevel => 4;

    /// <summary>Duel monster attribute (EARTH/WATER/FIRE/WIND/LIGHT/DARK). Override per card.</summary>
    public virtual DuelMonsterAttribute DuelMonsterAttribute => DuelMonsterAttribute.Earth;

    /// <summary>Duel monster race / type icon. <see cref="BaseMonsterCard"/> supplies the real value.</summary>
    public virtual DuelMonsterRace DuelMonsterRace => DuelMonsterRace.Warrior;

    /// <summary>If true, playing this monster card can summon a duel monster in a zone (max 5 per player).</summary>
    public virtual bool CanSummonDuelMonster => true;

    /// <summary>
    /// Monsters released for a normal tribute summon: 0 unless level 5+ non-ritual non-fusion (1 for level 5–6, 2 for 7+).
    /// Uses current <see cref="DuelMonsterLevel"/> (updates when level changes).
    /// </summary>
    public int TributeReleaseCount => ComputeTributeReleaseCount();

    /// <summary>Returns the correct description LocString for attack vs skill form. Used by description patch.</summary>
    public LocString GetDescriptionLocString()
    {
        string suffix;
        if (SupportsHandEffectForm && _displayForm == MonsterDisplayForm.HandEffect)
        {
            suffix = ".description_hand_effect";
            if (IsInHand())
                suffix += "_combat";
        }
        else
        {
            suffix = _displayForm == MonsterDisplayForm.Attack ? ".description" : ".description_skill";
            if (IsInHand())
                suffix += "_combat";
        }

        var baseLoc = new LocString("cards", Id.Entry + suffix);
        if (ShouldUseAlternateUpgradedDescription())
        {
            var upgraded = new LocString("cards", Id.Entry + suffix + "_upgraded");
            if (upgraded.Exists())
                return upgraded;
        }

        return baseLoc;
    }

    private bool ShouldUseAlternateUpgradedDescription()
    {
        if (!UseAlternateUpgradedDescription)
            return false;
        return IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None;
    }

    private bool IsInHand()
    {
        // Default must be compendium-safe: only switch to combat text when we're in an actual combat hand pile.
        if (CombatManager.Instance?.IsInProgress != true)
            return false;
        return Pile?.Type == PileType.Hand;
    }

    private bool IsRitualOrFusionMonster =>
        YgoCardType == YgoCardType.RitualMonster || YgoCardType == YgoCardType.FusionMonster;

    private IEnumerable<CardKeyword> GetFusionAndRitualKeywords()
    {
        if (YgoCardType == YgoCardType.FusionMonster)
            yield return FusionMonsterKeyword;
        else if (YgoCardType == YgoCardType.RitualMonster)
            yield return RitualMonsterKeyword;
    }

    private IEnumerable<CardKeyword> GetHandEffectMonsterKeywords()
    {
        if (SupportsHandEffectForm)
            yield return HandEffectMonsterKeyword;
    }

    /// <summary>
    /// Hint for attack/defense cycling when the card does not use the Hand Effect (20038) keyword.
    /// Only in main-deck flow piles — not monster zone, extra deck, graveyard browse as field pile, etc.
    /// </summary>
    private IEnumerable<CardKeyword> GetCycleMonsterKeywordWhenEligible()
    {
        if (SupportsHandEffectForm)
            yield break;
        if (Pile == null || !YgoCycleKeywordPileHelper.AllowsCycleKeywordHints(Pile.Type))
            yield break;
        yield return CycleMonsterKeyword;
    }

    private IEnumerable<CardKeyword> GetFlipEffectKeywords()
    {
        if (this is IMonsterFlipEffect)
            yield return FlipEffectKeyword;
    }

    protected virtual bool HasRecklessBlockerKeyword => false;

    private IEnumerable<CardKeyword> GetRecklessBlockerKeywords()
    {
        if (HasRecklessBlockerKeyword)
            yield return RecklessBlockerKeyword;
    }

    private int ComputeTributeReleaseCount()
    {
        if (IsRitualOrFusionMonster)
            return 0;

        int level = this is BaseMonsterCard bm ? bm.GetEffectiveDuelMonsterLevel() : DuelMonsterLevel;
        if (level < 5)
            return 0;
        if (level <= 6)
            return 1;
        return 2;
    }

    private IEnumerable<CardKeyword> GetSummonKeywordsByMonsterLevel()
    {
        int n = ComputeTributeReleaseCount();
        if (n == 1)
            yield return TributeSummon1Keyword;
        else if (n >= 2)
            yield return TributeSummon2Keyword;
    }

    private IEnumerable<CardKeyword> GetFaceDownKeywordsFromBool()
    {
        if (FaceDown)
            yield return FaceDownKeyword;
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            List<CardKeyword> keywords = new List<CardKeyword>(capacity: 2);
            keywords.Add(AttributeToKeyword(DuelMonsterAttribute));
            keywords.Add(RaceToKeyword(DuelMonsterRace));
            keywords.AddRange(GetFusionAndRitualKeywords());
            keywords.AddRange(GetHandEffectMonsterKeywords());
            keywords.AddRange(GetCycleMonsterKeywordWhenEligible());
            keywords.AddRange(GetFlipEffectKeywords());
            keywords.AddRange(GetRecklessBlockerKeywords());
            keywords.AddRange(GetFaceDownKeywordsFromBool());
            foreach (CardKeyword kw in GetSummonKeywordsByMonsterLevel())
                keywords.Add(kw);
            return keywords;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            List<IHoverTip> tips = new List<IHoverTip>(capacity: 2);
            tips.Add(HoverTipFactory.FromKeyword(AttributeToKeyword(DuelMonsterAttribute)));
            tips.Add(HoverTipFactory.FromKeyword(RaceToKeyword(DuelMonsterRace)));
            foreach (CardKeyword kw in GetFusionAndRitualKeywords())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (CardKeyword kw in GetHandEffectMonsterKeywords())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (CardKeyword kw in GetCycleMonsterKeywordWhenEligible())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            if (this is IMonsterFlipEffect)
            {
                var title = new LocString("card_keywords", "20041.title");
                var description = new LocString("cards", Id.Entry + ".flip_effect.description");
                tips.Add(new HoverTip(title, description));
            }
            foreach (CardKeyword kw in GetRecklessBlockerKeywords())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (CardKeyword kw in GetFaceDownKeywordsFromBool())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            foreach (CardKeyword kw in GetSummonKeywordsByMonsterLevel())
                tips.Add(HoverTipFactory.FromKeyword(kw));
            return tips;
        }
    }

    public void UpdateFaceDownKeywordFromBool()
    {
        if (!IsMutable)
            return;

        _ = Keywords;
        RemoveKeyword(FaceDownKeyword);
        foreach (CardKeyword kw in GetFaceDownKeywordsFromBool())
            AddKeyword(kw);

        DuelMonsterStancePowerSync.RequestSyncIfSummoned(this);
    }

    /// <summary>
    /// Recomputes <see cref="FaceDown"/> from the current display mode and <see cref="WillSet"/>.
    /// Useful after load/deserialize to avoid stale face-down overlays in hand UI.
    /// </summary>
    public void NormalizeFaceDownStateForCurrentDisplayMode()
    {
        // Preserve explicit face-down states (set/flip effects). Only recompute when not already face-down.
        if (!FaceDown)
        {
            bool shouldBeFaceDown = _displayForm == MonsterDisplayForm.Defense
                                    && WillSet
                                    && CanUseSetVisualStateInCurrentForm();
            FaceDown = shouldBeFaceDown;
        }
        UpdateFaceDownKeywordFromBool();
    }

    private bool CanUseSetVisualStateInCurrentForm()
    {
        if (YgoCardType == YgoCardType.FusionMonster)
            return false;
        if (SupportsHandEffectForm && _displayForm == MonsterDisplayForm.HandEffect)
            return false;
        return true;
    }

    /// <summary>
    /// Call this after changing <see cref="DuelMonsterLevel"/> so the card's keyword hover tooltips
    /// stay correct. Removes and re-applies 20033/20034/20035 based on current level.
    /// </summary>
    public void RefreshSummonKeywordsForMonsterLevel()
    {
        // Force init of the backing HashSet so RemoveKeyword/AddKeyword won't NRE.
        _ = Keywords;

        RemoveKeyword(SpecialSummonKeyword);
        RemoveKeyword(TributeSummon1Keyword);
        RemoveKeyword(TributeSummon2Keyword);

        foreach (CardKeyword kw in GetSummonKeywordsByMonsterLevel())
        {
            AddKeyword(kw);
        }
    }
}
