using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Gains Mgc ATK for each other face-up Machine-Type monster on your field.
/// </summary>
public sealed class Machine_King : EffectMonsterCard
{
    public Machine_King()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 22,
            baseDef: 20,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Machine;

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int others = 0;
        IReadOnlyCollection<BaseMonsterCard> field = DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner);
        foreach (BaseMonsterCard? m in field)
        {
            if (m == null || m.FaceDown || ReferenceEquals(m, this))
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Machine)
                others++;
        }

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (others * mgc, 0);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
