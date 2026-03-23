using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public abstract class FusionMonsterCard : EffectMonsterCard
{
    private readonly Type[] _fusionMaterialTypes;

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
        : base(cost, type, rarity, target, duelMonsterLevel, duelMonsterAttribute, baseAtk, baseDef, baseMgc, duelMonsterRace)
    {
        if (fusionMaterialTypes == null)
            throw new ArgumentNullException(nameof(fusionMaterialTypes));
        foreach (Type t in fusionMaterialTypes)
        {
            if (t == null || !typeof(BaseMonsterCard).IsAssignableFrom(t))
                throw new ArgumentException(
                    $"Fusion material type {t?.Name ?? "(null)"} must be a BaseMonsterCard subclass.",
                    nameof(fusionMaterialTypes));
        }

        _fusionMaterialTypes = fusionMaterialTypes.Length == 0 ? Array.Empty<Type>() : (Type[])fusionMaterialTypes.Clone();
    }

    /// <summary>Exact material card types (multiset); order does not matter for matching.</summary>
    public IReadOnlyList<Type> FusionMaterialTypes => _fusionMaterialTypes;

    protected override int MonsterConduitStarCost => 0;

    public override YgoCardType YgoCardType => YgoCardType.FusionMonster;

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
