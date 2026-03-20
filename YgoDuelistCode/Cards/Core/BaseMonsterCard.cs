using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// YgoDuelist monster card with base ATK/DEF/MGC stats. Mirrors Java BaseMonsterCard.
/// </summary>
public abstract class BaseMonsterCard : AbstractMonsterCard
{
    public override YgoCardType YgoCardType => YgoCardType.Monster;

    public int BaseAtk { get; }
    public int BaseDef { get; }
    public int BaseMgc { get; }

    /// <summary>Level (star count) for the duel monster this card summons.</summary>
    public override int DuelMonsterLevel { get; }

    /// <summary>Duel monster attribute (EARTH/WATER/FIRE/WIND/LIGHT/DARK) from the original YgoDuelist card.</summary>
    public override DuelMonsterAttribute DuelMonsterAttribute { get; }

    /// <summary>Duel monster race / type for the card frame icon.</summary>
    public override DuelMonsterRace DuelMonsterRace { get; }

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
        DuelMonsterRace duelMonsterRace = DuelMonsterRace.Warrior)
        : base(cost, type, rarity, target)
    {
        DuelMonsterLevel = duelMonsterLevel;
        DuelMonsterAttribute = duelMonsterAttribute;
        DuelMonsterRace = duelMonsterRace;
        BaseAtk = baseAtk;
        BaseDef = baseDef;
        BaseMgc = baseMgc;
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

        // Clamp like the Java version (0..9999).
        if (atk < 0) atk = 0;
        else if (atk > 9999) atk = 9999;

        if (def < 0) def = 0;
        else if (def > 9999) def = 9999;

        return new DuelMonsterStats(atk, def);
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
