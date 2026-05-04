using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned while you control another monster. Cannot Command Attack unless you control another Dragon-Type monster.
/// </summary>
public sealed class Cave_Dragon : EffectMonsterCard
{
    public Cave_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 20,
            baseDef: 1,
            baseMgc: 0,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Wind | YgoCardPackTags.Dragon;

    public override Type[] RelatedCards => new[] { typeof(Cave_Dragon) };

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => true;

    public override bool CanSummonDuelMonster =>
        Owner == null || DuelMonsterSummon.CountLiveDuelMonsters(Owner) == 0;

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;
            if (Owner == null)
                return true;
            return CanSummonDuelMonster;
        }
    }

    public override bool IsCommandAttackPlayable(Player? owner, Creature? pet)
    {
        Player? p = owner ?? Owner;
        if (p == null)
            return base.IsCommandAttackPlayable(owner, pet);

        bool otherDragon = DuelMonsterFieldRegistry
            .OrderedFieldMonsters(p)
            .Any(m =>
                m != null
                && !ReferenceEquals(m, this)
                && !m.FaceDown
                && m.DuelMonsterRace == DuelMonsterRace.Dragon);

        if (!otherDragon)
            return false;

        return base.IsCommandAttackPlayable(owner, pet);
    }
}
