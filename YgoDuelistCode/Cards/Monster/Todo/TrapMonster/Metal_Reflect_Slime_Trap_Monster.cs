using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.TrapMonster;

/// <summary>Trap Monster form of <see cref="Metal_Reflect_Slime"/> (only Special Summoned by that trap).</summary>
public sealed class Metal_Reflect_Slime_Trap_Monster : EffectMonsterCard
{
    public Metal_Reflect_Slime_Trap_Monster()
        : base(
            cost: 1,
            type: CardType.Skill,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 10,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 0,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.None;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => true;

    public override bool DuelMonsterExcludesCommandAttack => true;

    public override string PortraitPath => ModelDb.Card<Metal_Reflect_Slime>().PortraitPath;

    public override string CustomPortraitPath => ModelDb.Card<Metal_Reflect_Slime>().CustomPortraitPath;
}
