using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.TrapMonster;

/// <summary>Trap Monster form of <see cref="The_First_Monarch"/> (only Special Summoned by that trap).</summary>
public sealed class The_First_Monarch_Trap_Monster : EffectMonsterCard
{
    public The_First_Monarch_Trap_Monster()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 10,
            baseDef: 24,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.None;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => true;

    public override string PortraitPath => ModelDb.Card<The_First_Monarch>().PortraitPath;

    public override string CustomPortraitPath => ModelDb.Card<The_First_Monarch>().CustomPortraitPath;
}
