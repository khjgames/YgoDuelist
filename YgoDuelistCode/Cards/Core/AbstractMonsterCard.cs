using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
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
    private static CardKeyword TributeSummon3Keyword => (CardKeyword)20047;
    private static CardKeyword FusionMonsterKeyword => (CardKeyword)20036;
    private static CardKeyword RitualMonsterKeyword => (CardKeyword)20037;
    private static CardKeyword HandEffectMonsterKeyword => (CardKeyword)20038;
    private static CardKeyword CycleMonsterKeyword => (CardKeyword)20039;
    private static CardKeyword FlipEffectKeyword => (CardKeyword)20041;
    private static CardKeyword RecklessBlockerKeyword => (CardKeyword)20042;
    private static CardKeyword RecklessKeyword => (CardKeyword)20043;
    private static CardKeyword SplinterKeyword => (CardKeyword)20044;
    private static CardKeyword BlightKeyword => (CardKeyword)20045;
    private static CardKeyword PortionKeyword => (CardKeyword)20059;
    private static CardKeyword NamedFusionMaterialSubstituteKeyword => (CardKeyword)20061;
    private static CardKeyword SpiritMonsterKeyword => (CardKeyword)20067;

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
                int discount = Type == CardType.Attack
                    ? bm.GetDuelMonsterAttackPlayEnergyDiscount()
                    : bm.GetDuelMonsterDefensePlayEnergyDiscount();
                int discounted = discount <= 0 ? baseCost : baseCost - discount;
                if (discounted < 0)
                    discounted = 0;
                // Owner asserts mutable; canonical/library templates must not touch it (e.g. NCardGrid sort by EnergyCost).
                return YgoMonsterCommandEnergyModifiers.ApplyHandSummonFieldWideAddIfApplicable(bm, discounted);
            }

            return _handSummonFallbackEnergy;
        }
    }

    private readonly CardType _registeredCardType;

    protected AbstractMonsterCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
        _handSummonFallbackEnergy = cost;
        _registeredCardType = type;
        _displayForm = type == CardType.Attack ? MonsterDisplayForm.Attack : MonsterDisplayForm.Defense;
    }

    /// <summary>
    /// Attack vs defense from the card definition (constructor). Vanilla <see cref="EnchantmentModel.CanEnchant"/> uses
    /// <see cref="CardModel.Type"/>; for duel monsters that becomes <see cref="CardType.Skill"/> in defense position, so
    /// enchant eligibility for "Attack-only" enchantments uses this value instead.
    /// </summary>
    public CardType RegisteredCardType => _registeredCardType;

    /// <summary>When this card's duel monster pet dies — before option-pile cleanup (see DuelMonsterPetDeathPatch).</summary>
    public virtual Task OnPetDiedBeforeOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx) => Task.CompletedTask;

    /// <summary>When this card's duel monster pet dies — after option-pile cleanup, before GY relocation.</summary>
    public virtual Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx) => Task.CompletedTask;

    /// <summary>
    /// Sets whether this monster starts in attack position (Attack card) or defense position (Skill card).
    /// Called by derived classes once their stats (e.g. base ATK/DEF) are known.
    /// </summary>
    /// <param name="requestStanceSyncAfterKeywords">
    /// When false, skips <see cref="DuelMonsterStancePowerSync.RequestSyncIfSummoned"/> from keyword refresh (used by
    /// <see cref="ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync"/>, which awaits a single pet sync after hooks).
    /// </param>
    protected void SetDisplayAttackSkill(bool displayAsAttack, bool requestStanceSyncAfterKeywords = true)
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
        UpdateFaceDownKeywordFromBool(requestStanceSyncAfterKeywords);
        CardModelEnergyCache.Invalidate(this);
    }

    /// <summary>
    /// Sets attack vs defense position when the player uses <see cref="Command.Command_Attack"/> or <see cref="Command.Command_Defend"/>.
    /// Persists after the command resolves (same rules as <see cref="SetDisplayAttackSkill"/> for face-down).
    /// Does not run <see cref="OnSwitchedFromDefenseToAttackFromCommandAsync"/> / <see cref="OnSwitchedFromAttackToDefenseFromCommandAsync"/>; use
    /// <see cref="ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync"/> from those commands when switch effects should fire.
    /// </summary>
    public void SetBattlePositionFromDuelCommand(bool attackPosition) =>
        SetDisplayAttackSkill(attackPosition);

    /// <summary>
    /// Multiplayer: apply owner hand battle state deserialized from trailing <c>NetPlayCardAction</c> bits on observing
    /// peers (replaces inferring stance from <see cref="RegisteredCardType"/> alone).
    /// </summary>
    public void ApplyNetworkObserverHandPlayBattleState(
        bool attackPosition,
        bool handEffect,
        bool faceDownValue,
        bool willSetValue)
    {
        if (SupportsHandEffectForm && handEffect)
        {
            _displayForm = MonsterDisplayForm.HandEffect;
            FaceDown = false;
            WillSet = willSetValue;
        }
        else if (attackPosition)
        {
            _displayForm = MonsterDisplayForm.Attack;
            FaceDown = false;
            WillSet = willSetValue;
        }
        else
        {
            _displayForm = MonsterDisplayForm.Defense;
            FaceDown = faceDownValue;
            WillSet = willSetValue;
        }

        UpdateFaceDownKeywordFromBool();
        AfterDisplayFormChanged();
    }

    /// <summary>
    /// Copies only the preview/display form state. Used by card-grid upgraded clones so "View Upgrades" preserves
    /// the monster mode the player cycled to with right-click.
    /// </summary>
    public void CopyDisplayFormFrom(AbstractMonsterCard source)
    {
        ApplyNetworkObserverHandPlayBattleState(
            source.IsAttackBattlePosition,
            source.IsHandEffectFormActive,
            source.FaceDown,
            source.WillSet);
    }

    /// <summary>
    /// Same position update as <see cref="SetBattlePositionFromDuelCommand"/>, and when the stance actually changes, runs the same hooks as
    /// <see cref="Command.Command_Change_Battle_Position"/> (<see cref="OnSwitchedFromDefenseToAttackFromCommandAsync"/> /
    /// <see cref="OnSwitchedFromAttackToDefenseFromCommandAsync"/>).
    /// </summary>
    public async Task ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        bool attackPosition)
    {
        bool wasAttack = IsAttackBattlePosition;
        // Avoid RequestSyncIfSummoned racing SyncForPetAsync (double AttackPositionPower stacks on one peer).
        SetDisplayAttackSkill(attackPosition, requestStanceSyncAfterKeywords: false);
        if (wasAttack && !attackPosition)
            await OnSwitchedFromAttackToDefenseFromCommandAsync(choiceContext, player);
        else if (!wasAttack && attackPosition)
            await OnSwitchedFromDefenseToAttackFromCommandAsync(choiceContext, player);

        await DuelMonsterStancePowerSync.SyncSummonedPetIfPresentAsync(this, player.Creature);
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
    /// After this card's field monster switches from defense to attack via <see cref="ApplyBattlePositionChangeFromCommandMenu"/> or
    /// <see cref="ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync"/> (e.g. <see cref="Command.Command_Attack"/>).
    /// </summary>
    public virtual Task OnSwitchedFromDefenseToAttackFromCommandAsync(PlayerChoiceContext choiceContext, Player player) =>
        Task.CompletedTask;

    /// <summary>
    /// After this card's field monster switches from attack to defense via <see cref="ApplyBattlePositionChangeFromCommandMenu"/> or
    /// <see cref="ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync"/> (e.g. <see cref="Command.Command_Defend"/>).
    /// </summary>
    public virtual Task OnSwitchedFromAttackToDefenseFromCommandAsync(PlayerChoiceContext choiceContext, Player player) =>
        Task.CompletedTask;

    /// <summary>
    /// Face-down defense flipped to attack via <see cref="Command.Command_Change_Battle_Position"/> (command menu flip).
    /// Override for flip effects; default no-op.
    /// </summary>
    public virtual Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player) =>
        Task.CompletedTask;

    /// <summary>Cycles attack / defense, or attack / defense / hand effect when supported. Right-click in hand.</summary>
    /// <param name="allowCanonicalUiPreview">When true, canonical library/compendium instances may toggle (preview-only).</param>
    public void ToggleAttackSkill(bool allowCanonicalUiPreview = false)
    {
        if (IsCanonical && !allowCanonicalUiPreview)
            return;
        // Owner getter asserts mutable; canonical / library templates must not touch it (e.g. compendium right-click preview).
        if (this is BaseMonsterCard bm && IsMutable && MonsterCommandRegistry.SourceMonsterHasDieForYouForcedActive(Owner, bm))
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
        UpdatePortionKeywordFromDisplayForm();
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
    /// When true, special summon resolution may place a duel monster even if <see cref="CanSummonDuelMonster"/> is false
    /// (e.g. hand-effect form that disables normal summon for that toggle).
    /// </summary>
    public virtual bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => false;

    /// <summary>
    /// When true, <see cref="DuelMonsterSummon.TrySummonDuelMonsterSpecial"/> refuses this card (Cannot be Special Summoned).
    /// </summary>
    public virtual bool BlocksSpecialDuelMonsterSummon => false;

    /// <summary>
    /// When true, <see cref="DuelMonsterSummon.TrySummonDuelMonster"/> treats this hand summon like a special summon
    /// (Command Attack/Defend available the turn it hits the field). Default follows normal/tribute stiff/fatigue rules.
    /// </summary>
    public virtual bool SpecialSummonGrantsImmediateCommandsThisTurn => false;

    /// <summary>
    /// Tribute grid before a normal/tribute summon: remove illegal rows (e.g. Mausoleum placeholders for recipe-only summons).
    /// </summary>
    public virtual void FilterTributeSelectionGridCandidates(Player player, List<CardModel> candidates, int need)
    {
    }

    /// <summary>
    /// When false, <see cref="TributeSummonSelection"/> does not inject Mausoleum of the Emperor HP tribute rows for this summon.
    /// </summary>
    public virtual bool AllowsMausoleumHpTributeForThisTributeSummon => true;

    /// <summary>
    /// Monsters released for a normal tribute summon: 0 unless level 5+ non-ritual non-fusion (1 for level 5–6, 2 for 7+).
    /// Uses current <see cref="DuelMonsterLevel"/> (updates when level changes).
    /// </summary>
    public int TributeReleaseCount => ComputeTributeReleaseCount();

    /// <summary>When set, overrides level-based <see cref="TributeReleaseCount"/> (e.g. Egyptian God cards require 3).</summary>
    protected virtual int? TributeReleaseCountOverride => null;

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
        return IsUpgradedOrPreviewActive;
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

    /// <summary>Level 3+ normal-line monsters: <see cref="NormalMonsterCard"/>; drives Reckless keyword and CombatAction self-damage.</summary>
    protected virtual bool HasRecklessKeyword => false;

    /// <summary>How many <see cref="RecklessKeyword"/> entries appear in <see cref="CanonicalKeywords"/> (keyword text: 1 HP per stack).</summary>
    protected virtual int RecklessKeywordStackCountForDisplay =>
        HasRecklessKeyword ? 1 : 0;

    private IEnumerable<CardKeyword> GetRecklessKeywords()
    {
        int n = RecklessKeywordStackCountForDisplay;
        for (int i = 0; i < n; i++)
            yield return RecklessKeyword;
    }

    private IEnumerable<CardKeyword> GetRecklessBlockerKeywords()
    {
        if (HasRecklessBlockerKeyword)
            yield return RecklessBlockerKeyword;
    }

    private int ComputeTributeReleaseCount()
    {
        if (TributeReleaseCountOverride is int ovr)
            return ovr;

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
        else if (n == 2)
            yield return TributeSummon2Keyword;
        else if (n >= 3)
            yield return TributeSummon3Keyword;
    }

    private IEnumerable<CardKeyword> GetFaceDownKeywordsFromBool()
    {
        if (FaceDown)
            yield return FaceDownKeyword;
    }

    private IEnumerable<CardKeyword> GetSplinterBlightKeywordsFromMonster()
    {
        if (this is not BaseMonsterCard monster)
            yield break;
        if (monster.CardShowsSplinterKeyword)
            yield return SplinterKeyword;
        if (monster.CardShowsBlightKeyword)
            yield return BlightKeyword;
    }

    private IEnumerable<CardKeyword> GetPortionKeywordsFromMonster()
    {
        if (this is not BaseMonsterCard monster)
            yield break;
        if (!IsAttackBattlePosition)
            yield break;
        if (monster.GetResolvedAttackPortionCount() < 2)
            yield break;
        yield return PortionKeyword;
    }

    /// <summary>Attack/defense/hand-effect toggle: Portion chip only applies in attack form.</summary>
    public void UpdatePortionKeywordFromDisplayForm()
    {
        if (!IsMutable)
            return;

        _ = Keywords;
        RemoveKeyword(PortionKeyword);
        foreach (CardKeyword kw in GetPortionKeywordsFromMonster())
            AddKeyword(kw);
    }

    private IEnumerable<CardKeyword> GetNamedFusionMaterialSubstituteKeywordsFromMonster()
    {
        if (this is not IFusionMaterialSubstitute monster)
            yield break;
        if (monster.CanSubstituteAsFusionMaterial)
            yield return NamedFusionMaterialSubstituteKeyword;
    }

    private IEnumerable<CardKeyword> GetSpiritMonsterKeywords()
    {
        if (this is IYgoSpiritMonster)
            yield return SpiritMonsterKeyword;
    }

    private IEnumerable<CardKeyword> GetYgoArchetypeKeywords() =>
        YgoMonsterArchetypeKeywords.KeywordsForMonsterType(GetType());

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
            keywords.AddRange(GetRecklessKeywords());
            keywords.AddRange(GetFaceDownKeywordsFromBool());
            foreach (CardKeyword kw in GetSummonKeywordsByMonsterLevel())
                keywords.Add(kw);
            keywords.AddRange(GetSplinterBlightKeywordsFromMonster());
            keywords.AddRange(GetPortionKeywordsFromMonster());
            keywords.AddRange(GetNamedFusionMaterialSubstituteKeywordsFromMonster());
            keywords.AddRange(GetSpiritMonsterKeywords());
            keywords.AddRange(GetYgoArchetypeKeywords());
            return keywords;
        }
    }

    /// <remarks>
    /// Do not add <see cref="HoverTipFactory.FromKeyword"/> for entries already in <see cref="CanonicalKeywords"/>:
    /// <see cref="CardModel.HoverTips"/> appends <c>FromKeyword</c> for every <see cref="CardModel.Keywords"/> chip.
    /// Duplicating here produced two identical keyword tips (and mixed portrait vs non-portrait rendering).
    /// Mechanics with a dedicated power tip use <see cref="YgoDuelist.YgoDuelistCode.Services.YgoPowerOnlyMechanicKeywords"/> ids
    /// in <c>card_keywords.json</c> but must not be added to keyword chips — only <see cref="HoverTipFactory.FromPower{T}"/> in
    /// <see cref="ExtraHoverTips"/>.
    /// </remarks>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (this is IMonsterActivatedEffect ia)
            {
                var activateTitle = new LocString("card_keywords", "20051.title");
                var activateDesc = new LocString("cards", ia.ActivatedEffectDescriptionLocKey);
                DynamicVars.AddTo(activateDesc);
                UpgradeDisplay ifUpgradedDisplay = IsUpgradedOrPreviewActive
                    ? UpgradeDisplay.Upgraded
                    : UpgradeDisplay.Normal;
                activateDesc.Add(new IfUpgradedVar(ifUpgradedDisplay));
                yield return new HoverTip(activateTitle, activateDesc);
            }

            if (this is IMonsterSecondActivatedEffect ia2)
            {
                var activate2Title = new LocString("card_keywords", "20053.title");
                var activate2Desc = new LocString("cards", ia2.SecondActivatedEffectDescriptionLocKey);
                DynamicVars.AddTo(activate2Desc);
                UpgradeDisplay ifUpgradedDisplay2 = IsUpgradedOrPreviewActive
                    ? UpgradeDisplay.Upgraded
                    : UpgradeDisplay.Normal;
                activate2Desc.Add(new IfUpgradedVar(ifUpgradedDisplay2));
                yield return new HoverTip(activate2Title, activate2Desc);
            }

            foreach (IHoverTip tip in EnumerateReferencedCardPreviewHoverTips())
                yield return tip;
        }
    }

    /// <param name="requestStanceSync">
    /// When false, only refreshes <see cref="CardModel.Keywords"/> from <see cref="FaceDown"/> (used before MP checksum
    /// snapshots; stance is reconciled separately in <c>NetFullCombatStateYgoChecksumPatch</c>).
    /// </param>
    public void UpdateFaceDownKeywordFromBool(bool requestStanceSync = true)
    {
        if (!IsMutable)
            return;

        _ = Keywords;
        RemoveKeyword(FaceDownKeyword);
        foreach (CardKeyword kw in GetFaceDownKeywordsFromBool())
            AddKeyword(kw);

        if (requestStanceSync)
            DuelMonsterStancePowerSync.RequestSyncIfSummoned(this);
    }

    /// <summary>
    /// Recomputes <see cref="FaceDown"/> from the current display mode and <see cref="WillSet"/>.
    /// Useful after load/deserialize to avoid stale face-down overlays in hand UI.
    /// </summary>
    /// <param name="requestStanceSyncAfterKeyword">Passed to <see cref="UpdateFaceDownKeywordFromBool"/>.</param>
    /// <param name="preserveExplicitFaceDownWhenAlreadyTrue">
    /// When true (default), keeps <see cref="FaceDown"/> if already set so field flip/set effects are not cleared.
    /// When false, always sets <see cref="FaceDown"/> from display mode — used for hand/play piles before MP checksums
    /// so UI-only stale face-down (keyword 10012) cannot diverge across peers.
    /// </param>
    public void NormalizeFaceDownStateForCurrentDisplayMode(
        bool requestStanceSyncAfterKeyword = true,
        bool preserveExplicitFaceDownWhenAlreadyTrue = true)
    {
        if (!preserveExplicitFaceDownWhenAlreadyTrue || !FaceDown)
        {
            bool shouldBeFaceDown = _displayForm == MonsterDisplayForm.Defense
                                    && WillSet
                                    && CanUseSetVisualStateInCurrentForm();
            FaceDown = shouldBeFaceDown;
        }

        UpdateFaceDownKeywordFromBool(requestStanceSyncAfterKeyword);
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
    /// stay correct. Removes and re-applies 20033/20034/20035/20047 based on current level.
    /// </summary>
    public void RefreshSummonKeywordsForMonsterLevel()
    {
        // Force init of the backing HashSet so RemoveKeyword/AddKeyword won't NRE.
        _ = Keywords;

        RemoveKeyword(SpecialSummonKeyword);
        RemoveKeyword(TributeSummon1Keyword);
        RemoveKeyword(TributeSummon2Keyword);
        RemoveKeyword(TributeSummon3Keyword);

        foreach (CardKeyword kw in GetSummonKeywordsByMonsterLevel())
        {
            AddKeyword(kw);
        }
    }
}
