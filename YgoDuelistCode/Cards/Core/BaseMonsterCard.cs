using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
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

    private int _duelMonsterLevel;

    private readonly int? _duelMonsterAttackPlayEnergyOverride;
    private readonly int? _duelMonsterDefensePlayEnergyOverride;
    private readonly int _rawDuelMonsterAttackPlayEnergy;
    private readonly int _rawDuelMonsterDefensePlayEnergy;

    public int BaseAtk { get; }
    public int BaseDef { get; }
    public int BaseMgc { get; }

    private bool DuelMonsterPlayEnergyUpgradedOrPreview =>
        IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None;

    /// <summary>Energy to play from hand / summon in attack stance (Z = BaseAtk). High ATK low-level band may be 2 until upgraded.</summary>
    public int DuelMonsterAttackPlayEnergy => GetDuelMonsterAttackPlayEnergy(DuelMonsterPlayEnergyUpgradedOrPreview);

    /// <summary>Energy to play from hand / summon in defense stance (Z = BaseDef). High DEF low-level band may be 2 until upgraded.</summary>
    public int DuelMonsterDefensePlayEnergy => GetDuelMonsterDefensePlayEnergy(DuelMonsterPlayEnergyUpgradedOrPreview);

    /// <summary>Attack-stance play energy for a given upgraded/preview state (e.g. compendium without preview).</summary>
    public int GetDuelMonsterAttackPlayEnergy(bool upgradedOrPreview)
    {
        if (_duelMonsterAttackPlayEnergyOverride.HasValue)
            return _duelMonsterAttackPlayEnergyOverride.Value;
        int energy = MonsterEnergyCostCalculator.ApplyHighStatEfficiencyTax(
            _duelMonsterLevel,
            BaseAtk,
            isAttackStat: true,
            _rawDuelMonsterAttackPlayEnergy,
            upgradedOrPreview,
            DuelMonsterStatsAreUnknown);
        if (upgradedOrPreview && ZeroAttackPlayEnergyWhenUpgradedForLowAtkBlight)
            return 0;
        return energy;
    }

    /// <summary>Defense-stance play energy for a given upgraded/preview state.</summary>
    public int GetDuelMonsterDefensePlayEnergy(bool upgradedOrPreview)
    {
        if (_duelMonsterDefensePlayEnergyOverride.HasValue)
            return _duelMonsterDefensePlayEnergyOverride.Value;
        return MonsterEnergyCostCalculator.ApplyHighStatEfficiencyTax(
            _duelMonsterLevel,
            BaseDef,
            isAttackStat: false,
            _rawDuelMonsterDefensePlayEnergy,
            upgradedOrPreview,
            DuelMonsterStatsAreUnknown);
    }

    /// <summary>
    /// When true, printed ATK must not gain <see cref="YgoStatUpgradeScaling.GetMonsterPrintedStatUpgradeBonus"/> on upgrade — unupgraded attack stance is 2 energy from tax, upgraded is 1.
    /// </summary>
    protected bool SuppressPrintedAttackUpgradeForEfficiencyTax =>
        !_duelMonsterAttackPlayEnergyOverride.HasValue
        && MonsterEnergyCostCalculator.EfficiencyTaxRaisesPlayEnergyUnupgraded(
            _duelMonsterLevel, YgoCardType, BaseAtk, isAttackStat: true, DuelMonsterStatsAreUnknown);

    /// <summary>
    /// When true, printed DEF must not gain the normal upgrade bonus — unupgraded defense stance is 2 energy from tax, upgraded is 1.
    /// </summary>
    protected bool SuppressPrintedDefenseUpgradeForEfficiencyTax =>
        !_duelMonsterDefensePlayEnergyOverride.HasValue
        && MonsterEnergyCostCalculator.EfficiencyTaxRaisesPlayEnergyUnupgraded(
            _duelMonsterLevel, YgoCardType, BaseDef, isAttackStat: false, DuelMonsterStatsAreUnknown);

    /// <summary>Subtracts from attack/defense play energy (e.g. The Legendary Fisherman while Umi is up). Clamped to 0.</summary>
    public virtual int GetDuelMonsterPlayEnergyDiscount() => 0;

    /// <summary>Attack-stance discount: <see cref="GetDuelMonsterPlayEnergyDiscount"/> plus face-up equip attack discounts.</summary>
    public virtual int GetDuelMonsterAttackPlayEnergyDiscount() =>
        GetDuelMonsterPlayEnergyDiscount() + SumFaceUpEquipAttackDiscount() + SumLinkedTrapAttackPlayEnergyDiscount();

    /// <summary>Defense-stance discount: <see cref="GetDuelMonsterPlayEnergyDiscount"/> plus face-up equip defense discounts.</summary>
    public virtual int GetDuelMonsterDefensePlayEnergyDiscount() =>
        GetDuelMonsterPlayEnergyDiscount() + SumFaceUpEquipDefenseDiscount() + SumLinkedTrapDefensePlayEnergyDiscount();

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

    /// <summary>Duel monster attribute (EARTH/WATER/FIRE/WIND/LIGHT/DARK) from the original YgoDuelist card.</summary>
    public override DuelMonsterAttribute DuelMonsterAttribute { get; }

    /// <summary>Duel monster race / type for the card frame icon.</summary>
    public override DuelMonsterRace DuelMonsterRace { get; }

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
    public override bool CardShowsSplinterKeyword => AttackDealsSplinterDamage;

    /// <inheritdoc cref="YgoDuelistCard.CardShowsBlightKeyword" />
    public override bool CardShowsBlightKeyword => AttackDealsBlightedDamage;

    /// <summary>
    /// When true, Command Attack and Command Defend each use a separate once-per-turn allowance; stiff/fatigue applies after both are used.
    /// </summary>
    public virtual bool AllowsSeparateAttackAndDefendCommandsPerTurn => false;

    /// <summary>
    /// When true, normal/tribute summon does not apply stiff/fatigue for that turn (same timing as special summon).
    /// </summary>
    public virtual bool NormalSummonSkipsStiffFatigueOnSummonTurn => false;

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
        _rawDuelMonsterAttackPlayEnergy = MonsterEnergyCostCalculator.Compute(
            duelMonsterLevel, YgoCardType, baseAtk, DuelMonsterStatsAreUnknown);
        _rawDuelMonsterDefensePlayEnergy = MonsterEnergyCostCalculator.Compute(
            duelMonsterLevel, YgoCardType, baseDef, DuelMonsterStatsAreUnknown);

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

        StatEffectTotalMultiplier selfMult = GetSelfStatMultiplier();
        if (selfMult.Atk != 1m || selfMult.Def != 1m)
        {
            atk = (int)(atk * selfMult.Atk);
            def = (int)(def * selfMult.Def);
        }

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
        WingedMinionTributeAtkPower? wingedTribute = GetSourcePetWingedMinionTributeAtkPower();
        if (wingedTribute != null)
            atk += (int)wingedTribute.Amount;
        if (SourcePetHasPower<ReliableDefenderPower>())
            def += ReliableDefenderPower.DefBonus;

        // Owner getter asserts mutable; canonical/library card templates must not touch it.
        if (!IsCanonical && Owner != null)
        {
            if (Owner.Creature != null)
            {
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

            foreach (BaseEquipSpellCard equip in YgoEquipSpellRegistry.GetEquipsForMonster(this))
            {
                if (equip.Pile?.Type != SpellTrapZonePile.CustomType || equip.FaceDown)
                    continue;
                StatEffectTotalMultiplier em = equip.GetEquipStatMultiplier(this);
                if (em.Atk != 1m || em.Def != 1m)
                {
                    atk = (int)(atk * em.Atk);
                    def = (int)(def * em.Def);
                }
            }

            foreach (CardModel trap in YgoSpellTrapEquipLinkRegistry.GetLinkedTrapsForMonster(this))
            {
                if (trap is not IYgoSpellTrapEquipLinkStatEffect fx || !fx.IsSpellTrapEquipLinkStatEffectActive)
                    continue;
                StatEffectTotalMultiplier tm = fx.GetSpellTrapEquipLinkStatMultiplier();
                if (tm.Atk != 1m || tm.Def != 1m)
                {
                    atk = (int)(atk * tm.Atk);
                    def = (int)(def * tm.Def);
                }
            }

            foreach (BaseContinuousSpellCard continuous in YgoFieldSpellStatAggregator.GetActiveFaceUpContinuousSpells(Owner))
            {
                StatEffectTotal ce = continuous.GetContinuousStatEffect(this);
                atk += ce.BonusAtk;
                def += ce.BonusDef;
            }

            if (DuelMonsterRace == DuelMonsterRace.Machine && Owner.Creature != null)
            {
                LimiterRemovalPower? limiter = Owner.Creature.GetPower<LimiterRemovalPower>();
                if (limiter != null)
                {
                    StatEffectTotalMultiplier mult = limiter.MachineDuelMonsterStatMultiplier;
                    atk = (int)(atk * mult.Atk);
                    def = (int)(def * mult.Def);
                }
            }
        }

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
        else if (DynamicVars.Block != null)
            def = (int)DynamicVars.Block.BaseValue;
    }

    /// <summary>
    /// Printed level plus face-up field spell level modifiers (e.g. A Legendary Ocean), clamped 1–12 for UI and tribute rules.
    /// </summary>
    public int GetEffectiveDuelMonsterLevel()
    {
        int lv = DuelMonsterLevel;
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

        foreach (Creature pet in Owner.PlayerCombatState.Pets)
        {
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) == this && pet.HasPower<YgoCurseOfAnubisEffectMonsterPower>())
                return true;
        }

        return false;
    }

    private bool SourcePetHasPower<TPower>() where TPower : MegaCrit.Sts2.Core.Models.PowerModel
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return false;

        foreach (Creature pet in Owner.PlayerCombatState.Pets)
        {
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) == this && pet.HasPower<TPower>())
                return true;
        }

        return false;
    }

    private RushRecklesslyPower? GetSourcePetRushRecklesslyPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in Owner.PlayerCombatState.Pets)
        {
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) != this)
                continue;
            return pet.GetPower<RushRecklesslyPower>();
        }

        return null;
    }

    private WingedMinionTributeAtkPower? GetSourcePetWingedMinionTributeAtkPower()
    {
        if (IsCanonical || Owner?.PlayerCombatState == null)
            return null;

        foreach (Creature pet in Owner.PlayerCombatState.Pets)
        {
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) != this)
                continue;
            return pet.GetPower<WingedMinionTributeAtkPower>();
        }

        return null;
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
}
