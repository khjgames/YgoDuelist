using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Sword_Hunter : EffectMonsterCard
{
    public Sword_Hunter()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 24,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    /// <summary>+2 ATK (combat scale) per Warrior monster in your Graveyard.</summary>
    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return (0, 0);

        int n = GraveyardRelic.GetGraveyardCards(Owner).Count(c =>
            c is BaseMonsterCard m && m.DuelMonsterRace == DuelMonsterRace.Warrior);

        return (2 * n, 0);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
