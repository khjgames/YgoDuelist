using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Gains printed <c>Mgc</c> ATK for each Dragon monster you control (face-up) or in your Graveyard.</summary>
public sealed class Buster_Blader : EffectMonsterCard
{
    public Buster_Blader()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 26,
            baseDef: 23,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Earth | YgoCardPackTags.Warrior | YgoCardPackTags.Dragon;

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int dragons = 0;
        IReadOnlyCollection<BaseMonsterCard> field = DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner);
        foreach (BaseMonsterCard? m in field)
        {
            if (m == null || m.FaceDown)
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Dragon)
                dragons++;
        }

        foreach (CardModel c in YgoPlayerPiles.GraveyardCards(Owner))
        {
            if (c is BaseMonsterCard bm && bm.DuelMonsterRace == DuelMonsterRace.Dragon)
                dragons++;
        }

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (dragons * mgc, 0);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
    }
}
