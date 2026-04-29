using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// YgoDuelist monster card with base ATK/DEF/MGC stats. Field auras use <see cref="GetStatEffect"/>; own non-aura scaling uses <see cref="GetSecondaryStats"/>.
/// </summary>
/// <remarks>
/// <see cref="GetSecondaryStats"/> feeds the <c>CalculatedATK</c>/<c>CalculatedDEF</c> preview vars on <see cref="NormalMonsterCard"/> (e.g. Muka Muka hand size).
/// </remarks>
public abstract class BaseMonsterCard : AbstractMonsterCard
{
    public override YgoCardType YgoCardType => YgoCardType.Monster;

    /// <summary>
    /// When this card is played as <see cref="CardType.Skill"/> (defense stance or hand-effect form), vanilla uses this for
    /// targeting and <see cref="CardModel.IsValidTarget"/>. Default <see cref="TargetType.Self"/> for plain summons.
    /// Override to <see cref="TargetType.AnyEnemy"/>, <see cref="TargetType.AnyAlly"/>, etc. when the play applies a targeted
    /// effect (e.g. Weak on an enemy, heal on an ally) even from defense. You may branch on <see cref="AbstractMonsterCard.IsHandEffectFormActive"/>.
    /// </summary>
    protected virtual TargetType NonAttackPlayTargetType => TargetType.Self;

    /// <summary>
    /// Attack stance uses <see cref="TargetType.AnyEnemy"/>. Non-attack uses <see cref="NonAttackPlayTargetType"/>.
    /// </summary>
    public override TargetType TargetType =>
        Type == CardType.Attack ? TargetType.AnyEnemy : NonAttackPlayTargetType;

    private int _duelMonsterLevel;

    private readonly int? _duelMonsterAttackPlayEnergyOverride;
    private readonly int? _duelMonsterDefensePlayEnergyOverride;

    public int BaseAtk { get; }
    public int BaseDef { get; }
    public int BaseMgc { get; }

    /// <summary>
    /// Pack weight before +0.1 when this card type is a named fusion material and final [0.4, 2] clamp.
    /// <see cref="NormalMonsterCard"/> supplies tier for true normals; other monsters default to <c>1</c>.
    /// </summary>
    protected virtual float GetPackWeightMultiplierBase() => 1f;

    /// <inheritdoc />
    public override float PackWeightMultiplier
    {
        get
        {
            float w = GetPackWeightMultiplierBase();
            if (FusionMaterialArchetypeIndex.IsNamedFusionMaterial(GetType()))
                w += 0.1f;
            if (w < 0.4f)
                return 0.4f;
            if (w > 2f)
                return 2f;
            return w;
        }
    }

    private bool DuelMonsterPlayEnergyUpgradedOrPreview => IsUpgradedOrPreviewActive;

    /// <summary>Energy to play from hand / summon in attack stance (Z = printed ATK from <see cref="GetDynamicPrintedAtkDef"/>).</summary>
    public int DuelMonsterAttackPlayEnergy => GetDuelMonsterAttackPlayEnergy(DuelMonsterPlayEnergyUpgradedOrPreview);

    /// <summary>Energy to play from hand / summon in defense stance (Z = printed DEF from <see cref="GetDynamicPrintedAtkDef"/>).</summary>
    public int DuelMonsterDefensePlayEnergy => GetDuelMonsterDefensePlayEnergy(DuelMonsterPlayEnergyUpgradedOrPreview);

    /// <summary>Attack-stance play energy for a given upgraded/preview state (e.g. compendium without preview).</summary>
    public virtual int GetDuelMonsterAttackPlayEnergy(bool upgradedOrPreview)
    {
        if (_duelMonsterAttackPlayEnergyOverride.HasValue)
            return _duelMonsterAttackPlayEnergyOverride.Value;
        GetDynamicPrintedAtkDef(out int printedAtk, out _);
        int energy = MonsterEnergyCostCalculator.GetMonsterPlayEnergy(
            _duelMonsterLevel,
            YgoCardType,
            printedAtk,
            isAttackStat: true,
            upgradedOrPreview,
            DuelMonsterStatsAreUnknown,
            BaseAtk);
        if (upgradedOrPreview && ZeroAttackPlayEnergyWhenUpgradedForLowAtkBlight)
            return 0;
        return energy;
    }

    /// <summary>Defense-stance play energy for a given upgraded/preview state.</summary>
    public virtual int GetDuelMonsterDefensePlayEnergy(bool upgradedOrPreview)
    {
        if (_duelMonsterDefensePlayEnergyOverride.HasValue)
            return _duelMonsterDefensePlayEnergyOverride.Value;
        GetDynamicPrintedAtkDef(out _, out int printedDef);
        return MonsterEnergyCostCalculator.GetMonsterPlayEnergy(
            _duelMonsterLevel,
            YgoCardType,
            printedDef,
            isAttackStat: false,
            upgradedOrPreview,
            DuelMonsterStatsAreUnknown,
            BaseDef);
    }

    /// <summary>Cost Down and similar: −1 energy for monsters in hand while <see cref="CostDownHandLevelPower"/> is active.</summary>
    protected int GetCostDownHandPlayEnergyDiscount() =>
        !IsCanonical && Owner != null && Pile?.Type == PileType.Hand && Owner.Creature?.GetPower<CostDownHandLevelPower>() != null
            ? CostDownHandLevelPower.HandEnergyDiscount
            : 0;

    /// <summary>Subtracts from attack/defense play energy (e.g. The Legendary Fisherman while Umi is up). Clamped to 0.</summary>
    public virtual int GetDuelMonsterPlayEnergyDiscount() => GetCostDownHandPlayEnergyDiscount();

    /// <summary>Attack-stance discount: <see cref="GetDuelMonsterPlayEnergyDiscount"/> plus face-up equip attack discounts.</summary>
    public virtual int GetDuelMonsterAttackPlayEnergyDiscount() =>
        GetDuelMonsterPlayEnergyDiscount() + SumFaceUpEquipAttackDiscount() + SumLinkedTrapAttackPlayEnergyDiscount();

    /// <summary>Defense-stance discount: <see cref="GetDuelMonsterPlayEnergyDiscount"/> plus face-up equip defense discounts.</summary>
    public virtual int GetDuelMonsterDefensePlayEnergyDiscount() =>
        GetDuelMonsterPlayEnergyDiscount() + SumFaceUpEquipDefenseDiscount() + SumLinkedTrapDefensePlayEnergyDiscount();

    /// <summary>
    /// Intrinsic discount for field <c>Command_Attack</c> only. The monster may be ATK or DEF on field; default matches
    /// <see cref="GetDuelMonsterPlayEnergyDiscount"/>. Override when stance-gated hand rules should not hide an upgraded attack discount on Command Attack.
    /// </summary>
    public virtual int GetDuelMonsterPlayEnergyDiscountForFieldCommandAttack() =>
        GetDuelMonsterPlayEnergyDiscount();

    /// <summary>
    /// Intrinsic discount for field <c>Command_Defend</c> only. Default matches <see cref="GetDuelMonsterPlayEnergyDiscount"/>.
    /// Override when an attack-only stance bonus must not reduce Command Defend (see Terrorking Archfiend).
    /// </summary>
    public virtual int GetDuelMonsterPlayEnergyDiscountForFieldCommandDefend() =>
        GetDuelMonsterPlayEnergyDiscount();

    /// <summary>Command Attack row: <see cref="GetDuelMonsterPlayEnergyDiscountForFieldCommandAttack"/> plus face-up equip and linked trap attack discounts.</summary>
    public virtual int GetDuelMonsterAttackPlayEnergyDiscountForFieldCommand() =>
        GetDuelMonsterPlayEnergyDiscountForFieldCommandAttack()
        + SumFaceUpEquipAttackDiscount()
        + SumLinkedTrapAttackPlayEnergyDiscount();

    /// <summary>Command Defend row: <see cref="GetDuelMonsterPlayEnergyDiscountForFieldCommandDefend"/> plus face-up equip and linked trap defense discounts.</summary>
    public virtual int GetDuelMonsterDefensePlayEnergyDiscountForFieldCommand() =>
        GetDuelMonsterPlayEnergyDiscountForFieldCommandDefend()
        + SumFaceUpEquipDefenseDiscount()
        + SumLinkedTrapDefensePlayEnergyDiscount();

    /// <summary>Intrinsic reckless self-hit before each attack or block (normal line: level 3+ = 1).</summary>
    public virtual int GetIntrinsicRecklessCombatSelfDamage() => 0;

    protected override bool HasRecklessKeyword => GetTotalRecklessCombatSelfDamage() > 0;

    protected override int RecklessKeywordStackCountForDisplay
    {
        get
        {
            int d = GetTotalRecklessCombatSelfDamage();
            return d > 0 ? d : 0;
        }
    }

    /// <summary>Self-damage to the duel pet before attack/block from intrinsic reckless and face-up equips.</summary>
    public int GetTotalRecklessCombatSelfDamage() =>
        GetIntrinsicRecklessCombatSelfDamage() + SumFaceUpEquipRecklessSelfDamage();

    /// <summary>Level (star count) for the duel monster this card summons.</summary>
    public override int DuelMonsterLevel => _duelMonsterLevel;

    /// <summary>Set when this card resolves while face-down on the field (flip); cleared at the start of your turn.</summary>
    public bool FlippedThisTurn { get; set; }

    /// <summary>Duel monster attribute (EARTH/WATER/FIRE/WIND/LIGHT/DARK) from the original YgoDuelist card.</summary>
    public override DuelMonsterAttribute DuelMonsterAttribute { get; }

    /// <summary>Duel monster race / type for the card frame icon.</summary>
    public override DuelMonsterRace DuelMonsterRace { get; }

    /// <summary>
    /// 2–5: this monster's attack damage is dealt in that many hits that sum to its ATK (see <see cref="YgoDuelist.YgoDuelistCode.Services.YgoPortionMath"/>).
    /// Still <b>one logical attack</b> for Splinter/Blight aggregation and for <see cref="NormalMonsterCard.OnAfterMonsterAttackHitAsync"/> (fires once after the final chunk).
    /// Default <c>0</c> (off). Override on specific monster types when you want Portion.
    /// </summary>
    public virtual int AttackPortionCount => 0;

    /// <summary>Splinter (YGO piercing): after unblocked damage on an enemy, a decaying chain splashes other enemies (see <see cref="Relics.GraveyardRelic"/>).</summary>
    public virtual bool AttackDealsSplinterDamage => false;

    /// <summary>Blighted (YGO direct attack): 50% of hit damage (blocked and unblocked) applies as Blight stacks on the struck enemy (Blight X ticks at end of your turn, ignores Block, then removes).</summary>
    public virtual bool AttackDealsBlightedDamage => false;

    /// <summary>When true, blight attackers apply 100% of dealt damage as Blight instead of 50%.</summary>
    public virtual bool AttackDealsFullBlightedDamage => false;

    /// <summary>
    /// Low-ATK blight attackers: when upgraded (or upgrade preview), attack stance / Command Attack costs 0 energy.
    /// Printed ATK upgrade scaling is unchanged. (makes them worth comboing with atk boosts)
    /// </summary>
    protected virtual bool ZeroAttackPlayEnergyWhenUpgradedForLowAtkBlight =>
        AttackDealsBlightedDamage && BaseAtk <= 6;

    /// <inheritdoc cref="YgoDuelistCard.CardShowsSplinterKeyword" />
    public override bool CardShowsSplinterKeyword =>
        AttackDealsSplinterDamage || EnragedBattleOxService.MonsterCardShowsSplinterFromOx(this);

    /// <inheritdoc cref="YgoDuelistCard.CardShowsBlightKeyword" />
    public override bool CardShowsBlightKeyword => AttackDealsBlightedDamage;

    /// <inheritdoc cref="YgoDuelistCard.CardShowsPortionKeyword" />
    public override bool CardShowsPortionKeyword => AttackPortionCount >= 2;

    /// <summary>
    /// When true, Command Attack and Command Defend each use a separate once-per-turn allowance; stiff/fatigue applies after both are used.
    /// </summary>
    public virtual bool AllowsSeparateAttackAndDefendCommandsPerTurn => false;

    /// <summary>
    /// When true, normal/tribute summon does not apply stiff/fatigue for that turn (same timing as special summon).
    /// </summary>
    public virtual bool NormalSummonSkipsStiffFatigueOnSummonTurn => false;

    /// <summary>
    /// Called from <see cref="DuelMonsterSummon.TrySummonDuelMonster"/> right after the duel monster pet is created and
    /// <see cref="DuelMonsterFieldRegistry.RegisterSummon"/> runs. Override for summon-triggered effects.
    /// </summary>
    protected internal virtual Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        Task.CompletedTask;

    protected static PlayerChoiceContext EnsureBlockingChoiceContext(PlayerChoiceContext choiceContext) =>
        YgoChoiceContexts.Blocking(choiceContext);

    protected async Task RunOnSummonedAsync(
        Player player,
        PlayerChoiceContext choiceContext,
        Creature duelMonsterPet,
        Func<Task> resolveAsync)
    {
        await resolveAsync();
    }

    protected async Task RunOnNormalOrTributeSummonAsync(
        Player player,
        PlayerChoiceContext choiceContext,
        Creature duelMonsterPet,
        Func<PlayerChoiceContext, Task> resolveAsync)
    {
        if (YgoDuelMonsterSummonStyleContext.CurrentNormalOrTribute != true)
            return;

        await resolveAsync(EnsureBlockingChoiceContext(choiceContext));
    }

    protected Task RunOnFlipSummonedFromCommandMenuAsync(
        PlayerChoiceContext choiceContext,
        Func<PlayerChoiceContext, Task> resolveAsync) =>
        resolveAsync(EnsureBlockingChoiceContext(choiceContext));

    protected async Task ApplyConsumableShacklesOnSummonAsync(Player player, Creature duelMonsterPet)
    {
        if (player.Creature == null)
            return;

        if (IsUpgraded)
            await PowerCmd.Apply<ConsumableShacklesPlusPower>(duelMonsterPet, 1m, player.Creature, this);
        else
            await PowerCmd.Apply<ConsumableShacklesPower>(duelMonsterPet, 1m, player.Creature, this);
    }

    /// <summary>
    /// Right after <see cref="OnSummoned"/> in <see cref="DuelMonsterSummon.TrySummonDuelMonster"/> (before stumble/anubis/stiff). Hourglass, Hunter, Cure Mermaid.
    /// </summary>
    public virtual Task OnAfterSummonPipelineAsync(
        Player player,
        PlayerChoiceContext ctx,
        Creature pet,
        bool canAttackThisTurn) => Task.CompletedTask;

    /// <summary>
    /// After field relocation toward graveyard in <see cref="Patches.DuelMonsterPetDeathPatch"/>, before registries clear.
    /// Default: no-op. Override for powers that key off field presence (e.g. Enraged Battle Ox).
    /// </summary>
    public virtual Task OnAfterDuelMonsterPetDeathBeforeUnregisterAsync(Player player) => Task.CompletedTask;

    /// <summary>
    /// After <see cref="MegaCrit.Sts2.Core.Commands.CardPileCmd.Add"/> completes for this card.
    /// <paramref name="from"/> is the pile type before the move; <paramref name="newPileType"/> is the destination pile.
    /// </summary>
    public virtual void OnAfterPileMoveCompleted(Player? player, PileType? from, PileType newPileType)
    {
    }

    /// <summary>
    /// When true, Command Attack is omitted from the duel monster options menu (e.g. trap monsters that cannot attack).
    /// </summary>
    public virtual bool DuelMonsterExcludesCommandAttack => false;

    /// <summary>
    /// When true, <see cref="NormalMonsterCard.CombatAction"/> deals attack damage to every living enemy (same ATK per hit).
    /// </summary>
    public virtual bool DuelMonsterAttackHitsAllEnemies => false;

    /// <summary>
    /// ATK change per qualifying execute kill; applied via <see cref="ApplyPermanentExecuteAtkDelta"/> and persisted in <see cref="PermanentAtkBonusFromExecutes"/>.
    /// Exposed as <c>Increase</c> in <see cref="NormalMonsterCard.CanonicalVars"/> for <c>{Increase:diff()}</c> text (cf. <c>TheScythe</c>).
    /// </summary>
    public virtual int PermanentAtkDeltaOnEnemyExecute => 0;

    /// <summary>
    /// If false for a given kill, <see cref="PermanentAtkDeltaOnEnemyExecute"/> does not apply for that target (e.g. only real monsters).
    /// </summary>
    public virtual bool AppliesPermanentAtkDeltaOnEnemyKill(Creature killedEnemy) => true;

    /// <summary>
    /// Sum of ATK gained or lost from execute kills this run (same persistence pattern as <c>TheScythe</c> / <c>[SavedProperty]</c>).
    /// </summary>
    [SavedProperty]
    public int PermanentAtkBonusFromExecutes { get; set; }

    /// <summary>
    /// When true, the player enabled optional Die For You via <see cref="Command.Toggle_Die_For_You"/> (not Cure Mermaid forced).
    /// Serialized on the field monster card so multiplayer can reconcile <see cref="MegaCrit.Sts2.Core.Models.Powers.DieForYouPower"/> on the duel pet.
    /// </summary>
    [SavedProperty]
    public bool YgoDieForYouUserToggleOn { get; set; }

    /// <summary>
    /// Applies a permanent printed-ATK change from an execute kill and mirrors it to <see cref="CardModel.DeckVersion"/> when set.
    /// </summary>
    public void ApplyPermanentExecuteAtkDelta(int delta)
    {
        if (delta == 0)
            return;
        AssertMutable();
        PermanentAtkBonusFromExecutes += delta;
        if (DynamicVars?.Damage != null)
            DynamicVars.Damage.BaseValue += delta;
        if (DeckVersion is BaseMonsterCard deck && !ReferenceEquals(deck, this))
        {
            deck.PermanentAtkBonusFromExecutes += delta;
            if (deck.DynamicVars?.Damage != null)
                deck.DynamicVars.Damage.BaseValue += delta;
        }
    }

    /// <summary>
    /// Re-applies <see cref="PermanentAtkBonusFromExecutes"/> to <see cref="DynamicVars.Damage"/> (after full deserialize, downgrade, etc.).
    /// Uses canonical stats + <see cref="CardModel.CurrentUpgradeLevel"/> as the baseline, then adds the saved execute bonus (not a second += on top of an already-mutated Damage var).
    /// </summary>
    internal void ApplySavedExecuteAtkBonusToPrintedDamage()
    {
        if (PermanentAtkBonusFromExecutes == 0 || DynamicVars?.Damage == null)
            return;
        CardModel template = ModelDb.GetById<CardModel>(Id).ToMutable();
        for (int i = 0; i < CurrentUpgradeLevel; i++)
        {
            template.UpgradeInternal();
            template.FinalizeUpgradeInternal();
        }

        decimal baseline = template.DynamicVars.Damage.BaseValue;
        DynamicVars.Damage.BaseValue = baseline + PermanentAtkBonusFromExecutes;
        SyncPermanentExecuteIncreaseVar();
    }

    /// <summary>
    /// After <see cref="CardModel.FromSerializable"/> completes enchantments and upgrade; re-apply saved printed-stat bonuses
    /// that would otherwise be overwritten. Override when the card has additional saved bonuses beyond execute ATK.
    /// </summary>
    public virtual void ApplyPostDeserializePrintedStatBonuses() => ApplySavedExecuteAtkBonusToPrintedDamage();

    /// <summary>
    /// Multiplayer checksum: reconcile <see cref="DieForYouPower"/> on the duel pet before snapshot. Return <c>true</c> to skip
    /// generic optional-toggle reconciliation for this pet (e.g. Cure Mermaid forced Die For You).
    /// </summary>
    public virtual bool ReconcileDieForYouChecksumForPet(Creature pet, Player player) => false;

    /// <summary>When true, <see cref="YgoDuelist.YgoDuelistCode.Services.YgoEquipSpellTargetRules"/> skips equip race/restriction checks.</summary>
    public virtual bool IgnoresEquipSpellRaceRestrictions => false;

    /// <summary>
    /// End of owner turn: clear temporary field buffs tied to <see cref="YgoDuelist.YgoDuelistCode.Services.MonsterCommandRegistry.ClearPerTurnExtrasForPlayer"/>.
    /// </summary>
    public virtual void ClearTurnEndFieldBuffsFromMonsterCommandRegistry()
    {
    }

    /// <summary>Variable tribute grid minimum selection count (named triple recipes may require <paramref name="tributeNeed"/> picks).</summary>
    public virtual int MinTributeSelectionPickCount(int tributeNeed) => 1;

    /// <summary>Optional hook before generic <see cref="IMonsterFlipEffect"/> flip resolution (e.g. Hourglass of Courage).</summary>
    public virtual void ScheduleFlipFaceUpSideEffectsBeforeFlipPipeline()
    {
    }

    /// <summary>
    /// If true, this face-down <see cref="IMonsterFlipEffect"/> can appear in the "Activate Flip Effects" selection prompt
    /// when player-block hit flip checks run. If false, it flips immediately and is never listed in that prompt.
    /// </summary>
    public virtual bool AskSelectFlip => true;

    /// <summary>
    /// Keeps the <c>Increase</c> dynamic var aligned with <see cref="PermanentAtkDeltaOnEnemyExecute"/> after upgrade (cf. <c>TheScythe</c>).
    /// </summary>
    protected void SyncPermanentExecuteIncreaseVar()
    {
        if (PermanentAtkDeltaOnEnemyExecute == 0 || DynamicVars == null || !DynamicVars.ContainsKey("Increase"))
            return;
        DynamicVars["Increase"].BaseValue = PermanentAtkDeltaOnEnemyExecute;
    }

    /// <summary>
    /// Field aura: stat change this monster grants to <paramref name="target"/> while both are on the field (attribute, race, etc.).
    /// Default: no effect.
    /// </summary>
    public virtual StatEffectTotal GetStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    /// <summary>
    /// Optional extra ATK/DEF from this card's own secondary stats (e.g. hand-based scaling like Muka Muka).
    /// Default: 0/0; effect monsters can override.
    /// </summary>
    protected virtual (int atk, int def) GetSecondaryStats() => (0, 0);

    /// <summary>
    /// Multiplier on this monster's ATK/DEF after printed values and <see cref="GetSecondaryStats"/>, before field auras and other bonuses.
    /// </summary>
    protected virtual StatEffectTotalMultiplier GetSelfStatMultiplier() => StatEffectTotalMultiplier.Identity;

    /// <param name="duelMonsterAttackPlayEnergyOverride">When set, replaces <see cref="MonsterEnergyCostCalculator"/> for attack stance / Command Attack.</param>
    /// <param name="duelMonsterDefensePlayEnergyOverride">When set, replaces calculator for defense stance / Command Defend.</param>
    protected BaseMonsterCard(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace = DuelMonsterRace.Warrior,
        int? duelMonsterAttackPlayEnergyOverride = null,
        int? duelMonsterDefensePlayEnergyOverride = null)
        : base(cost, type, rarity, target)
    {
        DuelMonsterAttribute = duelMonsterAttribute;
        DuelMonsterRace = duelMonsterRace;
        BaseAtk = baseAtk;
        BaseDef = baseDef;
        BaseMgc = baseMgc;

        _duelMonsterLevel = duelMonsterLevel;

        _duelMonsterAttackPlayEnergyOverride = duelMonsterAttackPlayEnergyOverride;
        _duelMonsterDefensePlayEnergyOverride = duelMonsterDefensePlayEnergyOverride;

        // Start in defense position (Skill card) when DEF > ATK.
        // Equal stats keep the existing Attack default.
        SetDisplayAttackSkill(baseAtk >= baseDef);
    }

    /// <summary>
    /// Update the card's duel monster level, and refresh summon-type keywords (Normal vs Tribute)
    /// so hover tooltips and keyword UI stay correct.
    /// </summary>
    public void SetDuelMonsterLevel(int duelMonsterLevel)
    {
        _duelMonsterLevel = duelMonsterLevel;
        RefreshSummonKeywordsForMonsterLevel();
    }

    /// <summary>
    /// Calculates this monster's final ATK/DEF, applying support effects from all monsters on the field.
    /// Pass in all relevant monsters currently "in play" (including this one) to mirror the Java calcStats behavior.
    /// </summary>
    public DuelMonsterStats CalcDuelMonsterStats(IEnumerable<BaseMonsterCard> fieldMonsters)
    {
        GetDynamicPrintedAtkDef(out int atk, out int def);

        // Include any per-card secondary scaling (e.g. hand-based bonuses).
        var (secAtk, secDef) = GetSecondaryStats();
        atk += secAtk;
        def += secDef;

        // --- Additive phase: all flat ATK/DEF before any multipliers ---
        if (fieldMonsters != null)
        {
            foreach (BaseMonsterCard? source in fieldMonsters)
            {
                if (source == null)
                    continue;
                // Each monster on the field can contribute a flat ATK/DEF aura to this card.
                StatEffectTotal effect = source.GetStatEffect(this);
                atk += effect.BonusAtk;
                def += effect.BonusDef;
            }
        }

        RushRecklesslyPower? rush = GetSourcePetRushRecklesslyPower();
        if (rush != null)
            atk += (int)rush.Amount;

        RiryokuAtkShiftDonorPower? riryokuDonor = GetSourcePetRiryokuAtkShiftDonorPower();
        if (riryokuDonor != null)
            atk -= (int)riryokuDonor.Amount;
        RiryokuAtkShiftReceiverPower? riryokuRecv = GetSourcePetRiryokuAtkShiftReceiverPower();
        if (riryokuRecv != null)
            atk += (int)riryokuRecv.Amount;

        WingedMinionTributeAtkPower? wingedTribute = GetSourcePetWingedMinionTributeAtkPower();
        if (wingedTribute != null)
            atk += (int)wingedTribute.Amount;
        InsectPrincessExecuteAtkPower? insectPrincessExecute = GetSourcePetInsectPrincessExecuteAtkPower();
        if (insectPrincessExecute != null)
            atk += (int)insectPrincessExecute.Amount;
        LegendaryFiendAtkPower? legendaryFiend = GetSourcePetLegendaryFiendAtkPower();
        if (legendaryFiend != null)
            atk += (int)legendaryFiend.Amount;
        BazooSoulEaterTempAtkPower? bazooTemp = GetSourcePetBazooSoulEaterTempAtkPower();
        if (bazooTemp != null)
            atk += (int)bazooTemp.Amount;
        SpiritRyuTempAtkDefPower? spiritRyuTemp = GetSourcePetSpiritRyuTempAtkDefPower();
        if (spiritRyuTemp != null)
        {
            int b = (int)spiritRyuTemp.Amount;
            atk += b;
            def += b;
        }

        NecroticEvolutionPower? necroticEvolution = GetSourcePetNecroticEvolutionPower();
        if (necroticEvolution != null)
        {
            int n = (int)necroticEvolution.Amount;
            atk += n;
            def += n;
        }

        GearfriedIronKnightPower? gearfriedPow = GetSourcePetGearfriedIronKnightPower();
        if (gearfriedPow != null)
        {
            int g = (int)gearfriedPow.Amount;
            atk += g;
            def += g;
        }

        SlateWarriorPower? slateWarriorPow = GetSourcePetSlateWarriorPower();
        if (slateWarriorPow != null)
        {
            int s = (int)slateWarriorPow.Amount;
            atk += s;
            def += s;
        }

        atk += GetSourcePetSevenWeaponsAtkBonus();
        if (SourcePetHasPower<ReliableDefenderPower>())
            def += ReliableDefenderPower.DefBonus;

        // Owner getter asserts mutable; canonical/library card templates must not touch it.
        if (!IsCanonical && Owner != null)
        {
            if (Owner.Creature != null)
            {
                PyramidEnergyAtkBonusPower? pyramidAtk = Owner.Creature.GetPower<PyramidEnergyAtkBonusPower>();
                if (pyramidAtk != null)
                    atk += (int)pyramidAtk.Amount;
                PyramidEnergyDefBonusPower? pyramidDef = Owner.Creature.GetPower<PyramidEnergyDefBonusPower>();
                if (pyramidDef != null)
                    def += (int)pyramidDef.Amount;

                ReinforcementsPower? reinforcements = Owner.Creature.GetPower<ReinforcementsPower>();
                if (reinforcements != null)
                    atk += (int)reinforcements.Amount;
                CastleWallsPower? castleWalls = Owner.Creature.GetPower<CastleWallsPower>();
                if (castleWalls != null)
                    def += (int)castleWalls.Amount;
                GracefulDicePower? gracefulDice = Owner.Creature.GetPower<GracefulDicePower>();
                if (gracefulDice != null)
                {
                    int diceBonus = (int)gracefulDice.Amount;
                    atk += diceBonus;
                    def += diceBonus;
                }
            }

            foreach (BaseFieldSpellCard fieldSpell in YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(Owner))
            {
                StatEffectTotal fe = fieldSpell.GetFieldStatEffect(this);
                atk += fe.BonusAtk;
                def += fe.BonusDef;
            }

            foreach (BaseEquipSpellCard equip in YgoEquipSpellRegistry.GetEquipsForMonster(this))
            {
                if (equip.Pile?.Type != SpellTrapZonePile.CustomType || equip.FaceDown)
                    continue;
                StatEffectTotal ee = equip.GetEquipStatEffect(this);
                atk += ee.BonusAtk;
                def += ee.BonusDef;
            }

            foreach (BaseContinuousSpellCard continuous in YgoFieldSpellStatAggregator.GetActiveFaceUpContinuousSpells(Owner))
            {
                StatEffectTotal ce = continuous.GetContinuousStatEffect(this);
                atk += ce.BonusAtk;
                def += ce.BonusDef;
            }
        }

        // --- Multiplicative phase: self, equips, equip-link traps, Limiter Removal (product then one truncate) ---
        StatEffectTotalMultiplier multAtkDef = GetSelfStatMultiplier();
        decimal factorAtk = multAtkDef.Atk;
        decimal factorDef = multAtkDef.Def;

        if (!IsCanonical && Owner != null)
        {
            foreach (BaseEquipSpellCard equip in YgoEquipSpellRegistry.GetEquipsForMonster(this))
            {
                if (equip.Pile?.Type != SpellTrapZonePile.CustomType || equip.FaceDown)
                    continue;
                StatEffectTotalMultiplier em = equip.GetEquipStatMultiplier(this);
                factorAtk *= em.Atk;
                factorDef *= em.Def;
            }

            foreach (CardModel trap in YgoSpellTrapEquipLinkRegistry.GetLinkedTrapsForMonster(this))
            {
                if (trap is not IYgoSpellTrapEquipLinkStatEffect fx || !fx.IsSpellTrapEquipLinkStatEffectActive)
                    continue;
                StatEffectTotalMultiplier tm = fx.GetSpellTrapEquipLinkStatMultiplier();
                factorAtk *= tm.Atk;
                factorDef *= tm.Def;
            }

            if (DuelMonsterRace == DuelMonsterRace.Machine && Owner.Creature != null)
            {
                LimiterRemovalPower? limiter = Owner.Creature.GetPower<LimiterRemovalPower>();
                if (limiter != null)
                {
                    StatEffectTotalMultiplier mult = limiter.MachineDuelMonsterStatMultiplier;
                    factorAtk *= mult.Atk;
                    factorDef *= mult.Def;
                }
            }
        }

        atk = (int)(atk * factorAtk);
        def = (int)(def * factorDef);

        // Clamp like the Java version (0..9999).
        if (atk < 0) atk = 0;
        else if (atk > 9999) atk = 9999;

        if (def < 0) def = 0;
        else if (def > 9999) def = 9999;

        return new DuelMonsterStats(atk, def);
    }

    /// <summary>
    /// ATK/DEF as shown on the card: <see cref="DynamicVars"/> (upgrades, runtime changes) when present, else <see cref="BaseAtk"/>/<see cref="BaseDef"/>.
    /// </summary>
    protected void GetDynamicPrintedAtkDef(out int atk, out int def)
    {
        atk = BaseAtk;
        def = BaseDef;
        if (DynamicVars == null)
            return;
        if (DynamicVars.Damage != null)
            atk = (int)DynamicVars.Damage.BaseValue;
        if (DynamicVars.ContainsKey("Def"))
            def = (int)DynamicVars["Def"].BaseValue;
        else if (DynamicVars.ContainsKey("Block"))
            def = (int)DynamicVars["Block"].BaseValue;
    }

    /// <summary>
    /// Printed level plus face-up field spell level modifiers (e.g. A Legendary Ocean), clamped 1–12 for UI and tribute rules.
    /// </summary>
    public int GetEffectiveDuelMonsterLevel()
    {
        int lv = DuelMonsterLevel;
        if (!IsCanonical && Owner != null && Pile?.Type == PileType.Hand && Owner.Creature?.GetPower<CostDownHandLevelPower>() != null)
            lv -= CostDownHandLevelPower.LevelReduction;

        if (!IsCanonical && Owner != null)
        {
            foreach (BaseFieldSpellCard fieldSpell in YgoFieldSpellStatAggregator.GetActiveFaceUpFieldSpells(Owner))
            {
                StatEffectTotal fe = fieldSpell.GetFieldStatEffect(this);
                lv += fe.BonusLevel;
            }

            foreach (BaseContinuousSpellCard continuous in YgoFieldSpellStatAggregator.GetActiveFaceUpContinuousSpells(Owner))
            {
                StatEffectTotal ce = continuous.GetContinuousStatEffect(this);
                lv += ce.BonusLevel;
            }
        }

        if (lv < 1)
            lv = 1;
        else if (lv > 12)
            lv = 12;
        return lv;
    }

    /// <summary>Data for summoning a duel monster from this card (level, ATK, DEF, portrait path, name).</summary>
    public virtual DuelMonsterData GetDuelMonsterData()
    {
        GetDynamicPrintedAtkDef(out int atk, out int def);
        return new DuelMonsterData(
            DuelMonsterLevel,
            atk,
            def,
            "cards",
            Id.Entry + ".title",
            PortraitPath);
    }

    private bool SourcePetHasCurseOfAnubis()
    {
        if (this is not EffectMonsterCard || IsCanonical || Owner?.PlayerCombatState == null)
            return false;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (DuelMonsterFieldRegistry.HasSourceCard(pet, this) && pet.HasPower<YgoCurseOfAnubisEffectMonsterPower>())
                return true;
        }

        return false;
    }

    private bool SourcePetHasPower<TPower>() where TPower : MegaCrit.Sts2.Core.Models.PowerModel
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return false;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (DuelMonsterFieldRegistry.HasSourceCard(pet, this) && pet.HasPower<TPower>())
                return true;
        }

        return false;
    }

    private RushRecklesslyPower? GetSourcePetRushRecklesslyPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<RushRecklesslyPower>();
        }

        return null;
    }

    private RiryokuAtkShiftDonorPower? GetSourcePetRiryokuAtkShiftDonorPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<RiryokuAtkShiftDonorPower>();
        }

        return null;
    }

    private RiryokuAtkShiftReceiverPower? GetSourcePetRiryokuAtkShiftReceiverPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<RiryokuAtkShiftReceiverPower>();
        }

        return null;
    }

    private WingedMinionTributeAtkPower? GetSourcePetWingedMinionTributeAtkPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<WingedMinionTributeAtkPower>();
        }

        return null;
    }

    private InsectPrincessExecuteAtkPower? GetSourcePetInsectPrincessExecuteAtkPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<InsectPrincessExecuteAtkPower>();
        }

        return null;
    }

    private LegendaryFiendAtkPower? GetSourcePetLegendaryFiendAtkPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<LegendaryFiendAtkPower>();
        }

        return null;
    }

    private BazooSoulEaterTempAtkPower? GetSourcePetBazooSoulEaterTempAtkPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<BazooSoulEaterTempAtkPower>();
        }

        return null;
    }

    private SpiritRyuTempAtkDefPower? GetSourcePetSpiritRyuTempAtkDefPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<SpiritRyuTempAtkDefPower>();
        }

        return null;
    }

    private NecroticEvolutionPower? GetSourcePetNecroticEvolutionPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<NecroticEvolutionPower>();
        }

        return null;
    }

    private GearfriedIronKnightPower? GetSourcePetGearfriedIronKnightPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<GearfriedIronKnightPower>();
        }

        return null;
    }

    private SlateWarriorPower? GetSourcePetSlateWarriorPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return pet.GetPower<SlateWarriorPower>();
        }

        return null;
    }

    /// <summary><see cref="SevenWeaponsPower"/> / <see cref="SevenWeaponsPlusPower"/> ATK while at max stacks (any source that applies these powers).</summary>
    private int GetSourcePetSevenWeaponsAtkBonus()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return 0;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            if (pet.GetPower<SevenWeaponsPlusPower>() is { } plus)
                return plus.GetDuelMonsterAtkBonusFromStacks();
            if (pet.GetPower<SevenWeaponsPower>() is { } sw)
                return sw.GetDuelMonsterAtkBonusFromStacks();
            return 0;
        }

        return 0;
    }

    private int SumFaceUpEquipAttackDiscount()
    {
        if (IsCanonical || Owner == null)
            return 0;
        int sum = 0;
        foreach (BaseEquipSpellCard equip in YgoEquipSpellRegistry.GetEquipsForMonster(this))
        {
            if (equip.Pile?.Type != SpellTrapZonePile.CustomType || equip.FaceDown)
                continue;
            sum += equip.GetEquipAttackPlayEnergyDiscount(this);
        }
        return sum;
    }

    private int SumFaceUpEquipDefenseDiscount()
    {
        if (IsCanonical || Owner == null)
            return 0;
        int sum = 0;
        foreach (BaseEquipSpellCard equip in YgoEquipSpellRegistry.GetEquipsForMonster(this))
        {
            if (equip.Pile?.Type != SpellTrapZonePile.CustomType || equip.FaceDown)
                continue;
            sum += equip.GetEquipDefensePlayEnergyDiscount(this);
        }
        return sum;
    }

    private int SumFaceUpEquipRecklessSelfDamage()
    {
        if (IsCanonical || Owner == null)
            return 0;
        int sum = 0;
        foreach (BaseEquipSpellCard equip in YgoEquipSpellRegistry.GetEquipsForMonster(this))
        {
            if (equip.Pile?.Type != SpellTrapZonePile.CustomType || equip.FaceDown)
                continue;
            sum += equip.GetEquipRecklessCombatSelfDamage(this);
        }
        return sum;
    }

    private int SumLinkedTrapAttackPlayEnergyDiscount()
    {
        if (IsCanonical || Owner == null)
            return 0;
        int sum = 0;
        foreach (CardModel trap in YgoSpellTrapEquipLinkRegistry.GetLinkedTrapsForMonster(this))
        {
            if (trap is not IYgoSpellTrapEquipLinkStatEffect fx || !fx.IsSpellTrapEquipLinkStatEffectActive)
                continue;
            sum += fx.GetSpellTrapEquipLinkAttackPlayEnergyDiscount();
        }
        return sum;
    }

    private int SumLinkedTrapDefensePlayEnergyDiscount()
    {
        if (IsCanonical || Owner == null)
            return 0;
        int sum = 0;
        foreach (CardModel trap in YgoSpellTrapEquipLinkRegistry.GetLinkedTrapsForMonster(this))
        {
            if (trap is not IYgoSpellTrapEquipLinkStatEffect fx || !fx.IsSpellTrapEquipLinkStatEffectActive)
                continue;
            sum += fx.GetSpellTrapEquipLinkDefensePlayEnergyDiscount();
        }
        return sum;
    }

    /// <summary>When <see cref="DuelMonsterLevel"/> ≥ 8, registers for Deal with the Dark Ruler — opt out on specific cards (e.g. Berserk Dragon).</summary>
    protected virtual bool RegistersForLevel8DealWithDarkRulerWhenDestroyed => DuelMonsterLevel >= 8;

    /// <summary>Consume <see cref="YgoBattleDeathMarkedCards"/> when destroyed by battle (optional deck SS, Giant Germ, Lord Poison, etc.).</summary>
    protected virtual bool UsesBattleDeathGraveyardMark => this is IBattleDeathOptionalDeckSpecialSummon;

    /// <inheritdoc cref="AbstractMonsterCard.OnPetDiedBeforeOptionPileHandlingAsync" />
    public override Task OnPetDiedBeforeOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        if (DuelMonsterRace == DuelMonsterRace.Dragon)
            GraveyardRelic.RegisterDragonMonsterDestroyed(ctx.Player);

        if (RegistersForLevel8DealWithDarkRulerWhenDestroyed)
            YgoDealWithDarkRulerState.RegisterLevel8PlusMonsterSentToGraveyard(ctx.Player);

        if (DuelMonsterRace == DuelMonsterRace.Fairy
            && YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<The_Sanctuary_in_the_Sky>(ctx.Player))
        {
            bool dieForYou = ctx.Pet.HasPower<DieForYouPower>()
                || (ctx.CommandState != null && ctx.CommandState.DieForYouEnabled);
            if (dieForYou)
                GraveyardRelic.ArmSanctuaryHalveNextSpillDamage(ctx.Player);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="AbstractMonsterCard.OnPetDiedAfterOptionPileHandlingAsync" />
    public override Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        if (ctx.CommandState?.DestroyedByEnemyBattleDamage == true && UsesBattleDeathGraveyardMark)
            YgoBattleDeathMarkedCards.Mark(this);

        return Task.CompletedTask;
    }

    /// <summary>Enemy execute kills from this monster's attack (GraveyardRelic + splinter chain).</summary>
    public virtual Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs) => Task.CompletedTask;

    /// <summary>Top-level GraveyardRelic AfterAttack before splinter / on-damage (Spirit of the Breeze).</summary>
    public virtual Task OnGraveyardRelicAfterAttackOpeningAsync(
        AttackCommand command,
        Player? attackingPlayer,
        BlockingPlayerChoiceContext ctx) => Task.CompletedTask;

    /// <summary>First unblocked damage to each enemy in this attack chain (Cestus is equip; Bistro / Masked use this).</summary>
    public virtual Task OnFirstUnblockedDamageToEnemyThisChainAsync(
        AttackCommand command,
        DamageResult r,
        Player atkPlayer,
        BlockingPlayerChoiceContext ctx) => Task.CompletedTask;

    /// <summary>With Sanctuary field face-up, Mercury draw once per turn — override on The Agent of Wisdom Mercury.</summary>
    public virtual bool ParticipatesInSanctuaryMercuryDraw => false;

    /// <summary>GraveyardRelic owner turn start: per face-up field pet (e.g. Cure Mermaid maintenance).</summary>
    public virtual Task OnGraveyardRelicOwnerTurnStartForFieldPetAsync(
        PlayerChoiceContext ctx,
        Player player,
        Creature pet,
        GraveyardRelic relic) => Task.CompletedTask;

    /// <summary>GraveyardRelic owner turn start: for each copy of this card in your GY (e.g. Darklord Marie).</summary>
    public virtual Task OnGraveyardRelicOwnerTurnStartWhileInGraveyardAsync(
        PlayerChoiceContext ctx,
        Player player,
        GraveyardRelic relic) => Task.CompletedTask;

    /// <summary>End of controlling player turn: field cleanup before per-turn command flags clear (Karate Man, Guardian Slime).</summary>
    public virtual Task OnOwnerTurnEndFieldCleanupAsync(
        PlayerChoiceContext ctx,
        Player owner,
        Creature pet) => Task.CompletedTask;

    /// <summary>
    /// After this monster was moved to the graveyard from hand or field (<paramref name="from"/> is the pre-move source pile).
    /// Invoked only for those sources; default no-op.
    /// </summary>
    public virtual void OnMovedToGraveyardFromHandOrField(PileType from) { }

    /// <summary>
    /// Hand → GY: draw cards equal to printed <c>Mgc</c>. Used by <see cref="Cards.Monster.Todo.Effect.Electric_Snake"/>, <see cref="Cards.Monster.Todo.Effect.Elephant_Statue_of_Blessing"/>.
    /// </summary>
    protected void ScheduleDrawCardsEqualToPrintedMgcWhenMovedFromHandToGraveyard(PileType from)
    {
        if (from != PileType.Hand)
            return;
        Player? player = Owner;
        if (player?.Creature?.CombatState == null)
            return;
        decimal n = DynamicVars["Mgc"].BaseValue;
        if (n <= 0m)
            return;
        TaskHelper.RunSafely(DrawCardsForPrintedMgcAsync(player, n));
    }

    private static async Task DrawCardsForPrintedMgcAsync(Player player, decimal n)
    {
        var ctx = YgoChoiceContexts.Blocking();
        await CardPileCmd.Draw(ctx, n, player);
    }

    /// <summary>Extra max HP from Fortified Beasts sync (Command Knight).</summary>
    public virtual int GetFortifiedBeastsBonusMaxHp(Player player) => 0;

    /// <summary>When true with face-down defense, Command Attack may deal flip damage before stance change (Stealth Bird).</summary>
    public virtual bool UsesFaceDownFlipDamageOnCommandAttack => false;

    /// <summary>Extra Command Attack playability (Dark Zebra, Ultimate Obedient Fiend).</summary>
    public virtual bool IsCommandAttackPlayable(Player? owner, Creature? pet) => true;

    /// <summary>Stance sync: grant/strip player thorns tied to field presence (e.g. Amazoness Swords Woman).</summary>
    public virtual Task SyncPlayerThornsFromFieldPetPresenceAsync(Creature pet, Creature? applier, CardModel? sourceCard) =>
        Task.CompletedTask;

    /// <summary>Before this card leaves the field: remove thorns granted via <see cref="SyncPlayerThornsFromFieldPetPresenceAsync"/>.</summary>
    public virtual Task StripPlayerThornsGrantedFromFieldPresenceAsync(Creature playerCreature) => Task.CompletedTask;

    /// <summary>After stance sync on Command Attack, before main hit (Stealth Bird flip, Gravekeeper's Assailant).</summary>
    public virtual Task OnCommandAttackAfterStanceSyncedAsync(
        PlayerChoiceContext ctx,
        Player player,
        Creature? pet,
        CardPlay cardPlay,
        bool stealthBirdWasFaceDownDefenseBeforeCommandAttack) => Task.CompletedTask;
}
