using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;

public sealed class Dark_Paladin : FusionMonsterCard
{
    public Dark_Paladin()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 29,
            baseDef: 24,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Spellcaster,
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal.Dark_Magician),
            typeof(global::YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect.Buster_Blader))
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Dragon;

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int dragons = 0;
        IReadOnlyCollection<BaseMonsterCard> field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner);
        foreach (BaseMonsterCard? m in field)
        {
            if (m == null || m.FaceDown)
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Dragon)
                dragons++;
        }

        foreach (CardModel c in GraveyardRelic.GetGraveyardCards(Owner))
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
