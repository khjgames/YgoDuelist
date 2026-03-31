using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public abstract class FusionMonsterCard : EffectMonsterCard
{
    private readonly FusionMaterialSlot[] _fusionMaterialSlots;

    /// <summary>Use <see cref="FusionMonsterCard"/> with <c>params</c> when no duel energy overrides are needed.</summary>
    protected FusionMonsterCard(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace,
        Type[] fusionMaterialTypes,
        int? duelMonsterAttackPlayEnergyOverride = null,
        int? duelMonsterDefensePlayEnergyOverride = null)
        : this(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace,
            MapNamedTypesToSlots(fusionMaterialTypes), duelMonsterAttackPlayEnergyOverride, duelMonsterDefensePlayEnergyOverride)
    {
    }

    /// <summary>Full fusion recipe (named slots, requirement slots, or mixed).</summary>
    protected FusionMonsterCard(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace,
        FusionMaterialSlot[] fusionMaterialSlots,
        int? duelMonsterAttackPlayEnergyOverride = null,
        int? duelMonsterDefensePlayEnergyOverride = null)
        : base(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace,
            duelMonsterAttackPlayEnergyOverride, duelMonsterDefensePlayEnergyOverride)
    {
        ArgumentNullException.ThrowIfNull(fusionMaterialSlots);
        _fusionMaterialSlots = (FusionMaterialSlot[])fusionMaterialSlots.Clone();
        WillSet = false;
        FaceDown = false;
        SetDisplayAttackSkill(displayAsAttack: false);
    }

    protected FusionMonsterCard(
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
        params Type[] fusionMaterialTypes)
        : this(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace,
            fusionMaterialTypes, null, null)
    {
    }

    /// <summary>params <see cref="FusionMaterialSlot"/> recipe (after stats).</summary>
    protected FusionMonsterCard(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace,
        params FusionMaterialSlot[] fusionMaterialSlots)
        : this(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace,
            fusionMaterialSlots, null, null)
    {
    }

    private static FusionMaterialSlot[] MapNamedTypesToSlots(Type[] fusionMaterialTypes)
    {
        ArgumentNullException.ThrowIfNull(fusionMaterialTypes);
        foreach (Type t in fusionMaterialTypes)
        {
            if (t == null || !typeof(BaseMonsterCard).IsAssignableFrom(t))
                throw new ArgumentException(
                    $"Fusion material type {t?.Name ?? "(null)"} must be a BaseMonsterCard subclass.",
                    nameof(fusionMaterialTypes));
        }

        var slots = new FusionMaterialSlot[fusionMaterialTypes.Length];
        for (int i = 0; i < fusionMaterialTypes.Length; i++)
            slots[i] = FusionMaterialSlot.ForNamed(fusionMaterialTypes[i]);
        return slots;
    }

    /// <summary>
    /// Fusion recipe (multiset); order does not matter for matching.
    /// </summary>
    public IReadOnlyList<FusionMaterialSlot> FusionMaterialSlots => _fusionMaterialSlots;

    protected override int MonsterConduitStarCost => 0;

    public override YgoCardType YgoCardType => YgoCardType.FusionMonster;

    /// <summary>Pack filtering: fusion frame plus attribute and race-derived tags (see <see cref="PackTagsForFusionProfile"/>).</summary>
    public override YgoCardPackTags PackTags => YgoCardPackTags.Fusion | PackTagsForFusionProfile(DuelMonsterAttribute, DuelMonsterRace);

    /// <summary>Maps printed attribute/race to <see cref="YgoCardPackTags"/> bits for card packs.</summary>
    internal static YgoCardPackTags PackTagsForFusionProfile(DuelMonsterAttribute attribute, DuelMonsterRace race)
    {
        YgoCardPackTags tags = AttributeToPackTag(attribute) | RaceToPackTag(race);
        return tags;
    }

    private static YgoCardPackTags AttributeToPackTag(DuelMonsterAttribute attribute) =>
        attribute switch
        {
            DuelMonsterAttribute.Earth => YgoCardPackTags.Earth,
            DuelMonsterAttribute.Water => YgoCardPackTags.Water,
            DuelMonsterAttribute.Wind => YgoCardPackTags.Wind,
            DuelMonsterAttribute.Fire => YgoCardPackTags.Fire,
            DuelMonsterAttribute.Dark => YgoCardPackTags.Dark,
            DuelMonsterAttribute.Light => YgoCardPackTags.Light,
            _ => YgoCardPackTags.None
        };

    private static YgoCardPackTags RaceToPackTag(DuelMonsterRace race) =>
        race switch
        {
            DuelMonsterRace.Dragon => YgoCardPackTags.Dragon,
            DuelMonsterRace.Wyrm => YgoCardPackTags.Dragon,
            DuelMonsterRace.Machine => YgoCardPackTags.Machine,
            DuelMonsterRace.Zombie => YgoCardPackTags.Zombie,
            DuelMonsterRace.Fiend => YgoCardPackTags.Fiend,
            DuelMonsterRace.Spellcaster => YgoCardPackTags.Spellcaster,
            DuelMonsterRace.Warrior => YgoCardPackTags.Warrior,
            DuelMonsterRace.BeastWarrior => YgoCardPackTags.Warrior,
            DuelMonsterRace.Insect => YgoCardPackTags.Insect,
            DuelMonsterRace.Aqua => YgoCardPackTags.Ocean,
            DuelMonsterRace.Fish => YgoCardPackTags.Ocean,
            DuelMonsterRace.SeaSerpent => YgoCardPackTags.Ocean,
            DuelMonsterRace.DivineBeast => YgoCardPackTags.God,
            DuelMonsterRace.Pyro => YgoCardPackTags.Burn,
            DuelMonsterRace.Thunder => YgoCardPackTags.Wind,
            DuelMonsterRace.Rock => YgoCardPackTags.Earth,
            _ => YgoCardPackTags.None
        };

    /// <summary>Fusion monsters are only summoned via fusion spells, not played from the hand.</summary>
    protected override bool IsPlayable
    {
        get
        {
            if (CombatManager.Instance?.IsInProgress == true && Pile?.Type == PileType.Hand)
                return false;
            return base.IsPlayable;
        }
    }
}
