using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
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
        return MonsterEnergyCostCalculator.ApplyHighStatEfficiencyTax(
            _duelMonsterLevel,
            BaseAtk,
            isAttackStat: true,
            _rawDuelMonsterAttackPlayEnergy,
            upgradedOrPreview,
            DuelMonsterStatsAreUnknown);
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

    /// <summary>Level (star count) for the duel monster this card summons.</summary>
    public override int DuelMonsterLevel => _duelMonsterLevel;

    /// <summary>Duel monster attribute (EARTH/WATER/FIRE/WIND/LIGHT/DARK) from the original YgoDuelist card.</summary>
    public override DuelMonsterAttribute DuelMonsterAttribute { get; }

    /// <summary>Duel monster race / type for the card frame icon.</summary>
    public override DuelMonsterRace DuelMonsterRace { get; }

    /// <summary>Splinter (YGO piercing): when this deals unblocked damage, each other enemy takes 50% of that damage.</summary>
    public virtual bool AttackDealsSplinterDamage => false;

    /// <summary>Blighted (YGO direct attack): 50% of unblocked hit damage applies as Blight stacks on the hit enemy (Blight X ticks at end of your turn, ignores Block, then removes).</summary>
    public virtual bool AttackDealsBlightedDamage => false;

    /// <summary>
    /// ATK permanently added to <see cref="NormalMonsterCard.DynamicVars"/>.Damage when this card's duel monster kills an enemy with an attack (revised execute effects).
    /// </summary>
    public virtual int PermanentAtkDeltaOnEnemyExecute => 0;

    /// <summary>
    /// If false for a given kill, <see cref="PermanentAtkDeltaOnEnemyExecute"/> does not apply for that target (e.g. only real monsters).
    /// </summary>
    public virtual bool AppliesPermanentAtkDeltaOnEnemyKill(Creature killedEnemy) => true;

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
        if (SourcePetHasPower<ReliableDefenderPower>())
            def += ReliableDefenderPower.DefBonus;

        // Owner getter asserts mutable; canonical/library card templates must not touch it.
        if (!IsCanonical && Owner != null)
        {
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
    private void GetDynamicPrintedAtkDef(out int atk, out int def)
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
    public virtual DuelMonsterData GetDuelMonsterData() =>
        new DuelMonsterData(
            DuelMonsterLevel,
            BaseAtk,
            BaseDef,
            "cards",
            Id.Entry + ".title",
            PortraitPath);

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
}
