using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Amazoness_Tiger : EffectMonsterCard
{
    public Amazoness_Tiger()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 11,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    /// <summary>+4 ATK per other face-up Amazoness on your field (400 ATK YGO scale / 100).</summary>
    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return (0, 0);

        var field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner)?.ToList() ?? new List<BaseMonsterCard>();
        int n = field.Count(m =>
            m != this &&
            m.Id.Entry.Contains("AMAZONESS", StringComparison.OrdinalIgnoreCase));

        return (4 * n, 0);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
