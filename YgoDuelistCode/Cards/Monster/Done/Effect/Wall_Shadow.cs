using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Special Summon only while <see cref="YgoMagicalLabyrinthWallShadowSummonState"/> is active
/// (<see cref="YgoDuelist.YgoDuelistCode.Cards.Command.Special_Summon_Wall_Shadow"/> on <see cref="Labyrinth_Wall"/> equipped with <see cref="Magical_Labyrinth"/>).
/// </summary>
public sealed class Wall_Shadow : EffectMonsterCard
{
    public Wall_Shadow()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 16,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterAttackPlayEnergyOverride: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Earth | YgoCardPackTags.Dark | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards =>
        new[] { typeof(Wall_Shadow), typeof(Magical_Labyrinth), typeof(Labyrinth_Wall) };
    
    public override Type[] BundledCards => new[] { typeof(Magical_Labyrinth) };

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate =>
        YgoMagicalLabyrinthWallShadowSummonState.IsSummonBypassActive;

    /// <summary>
    /// <see cref="NormalMonsterCard.IsPlayable"/> returns true when <see cref="CanSummonDuelMonster"/> is false (hand summon checks are skipped).
    /// This monster must not be played from hand at all — only special summoned via <see cref="Magical_Labyrinth"/> command — same pattern as
    /// <see cref="Berserk_Dragon"/>.
    /// </summary>
    protected override bool IsPlayable => base.IsPlayable && CanSummonDuelMonster;
}
