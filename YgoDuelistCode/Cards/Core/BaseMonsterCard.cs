using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// YgoDuelist monster card with base ATK/DEF/MGC stats. Mirrors Java BaseMonsterCard.
/// </summary>
public abstract class BaseMonsterCard : AbstractMonsterCard
{
    public override YgoCardType YgoCardType => YgoCardType.Monster;

    private int _duelMonsterLevel;

    public int BaseAtk { get; }
    public int BaseDef { get; }
    public int BaseMgc { get; }

    /// <summary>Energy to play from hand / summon in attack stance (Z = BaseAtk).</summary>
    public int DuelMonsterAttackPlayEnergy { get; }

    /// <summary>Energy to play from hand / summon in defense stance (Z = BaseDef).</summary>
    public int DuelMonsterDefensePlayEnergy { get; }

    /// <summary>Level (star count) for the duel monster this card summons.</summary>
    public override int DuelMonsterLevel => _duelMonsterLevel;

    /// <summary>Duel monster attribute (EARTH/WATER/FIRE/WIND/LIGHT/DARK) from the original YgoDuelist card.</summary>
    public override DuelMonsterAttribute DuelMonsterAttribute { get; }

    /// <summary>Duel monster race / type for the card frame icon.</summary>
    public override DuelMonsterRace DuelMonsterRace { get; }

    /// <summary>Mod keyword Splinter (YGO piercing): unblocked damage to the primary target splashes 50% to other enemies.</summary>
    public virtual bool AttackDealsSplinterDamage => false;

    /// <summary>Mod keyword Blighted (YGO direct-attack style): 50% of unblocked hit damage applies as Blight stacks on the target.</summary>
    public virtual bool AttackDealsBlightedDamage => false;

    /// <summary>
    /// ATK permanently added to <see cref="NormalMonsterCard.DynamicVars"/>.Damage when this card's duel monster kills an enemy with an attack (revised execute effects).
    /// </summary>
    public virtual int PermanentAtkDeltaOnEnemyExecute => 0;

    /// <summary>
    /// Support effect this monster applies to a target duel monster based on its attribute (e.g. +MGC ATK to same-attribute, -4 to the opposing attribute).
    /// Default: no effect.
    /// </summary>
    public virtual StatEffectTotal GetStatEffect(DuelMonsterAttribute targetAttribute) => StatEffectTotal.None;

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

        DuelMonsterAttackPlayEnergy = duelMonsterAttackPlayEnergyOverride ?? MonsterEnergyCostCalculator.Compute(
            duelMonsterLevel, YgoCardType, baseAtk, DuelMonsterStatsAreUnknown);
        DuelMonsterDefensePlayEnergy = duelMonsterDefensePlayEnergyOverride ?? MonsterEnergyCostCalculator.Compute(
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
        int atk = BaseAtk;
        int def = BaseDef;

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
                // Each monster on the field can contribute a flat ATK/DEF change for this attribute.
                StatEffectTotal effect = source.GetStatEffect(DuelMonsterAttribute);
                atk += effect.BonusAtk;
                def += effect.BonusDef;
            }
        }

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
}
