using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

public sealed class Megamorph : BaseEquipSpellCard
{
    public Megamorph()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) => StatEffectTotal.None;

    public override StatEffectTotalMultiplier GetEquipStatMultiplier(BaseMonsterCard equipped) =>
        ResolveMultiplier(equipped.Owner);

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    /// <summary>
    /// Double ATK when your HP% is below the aggregate HP% of all living enemies; otherwise halve ATK.
    /// Aggregate enemy HP% = sum(current HP) / sum(max HP) over living enemies.
    /// </summary>
    private static StatEffectTotalMultiplier ResolveMultiplier(Player? player)
    {
        Creature? duelist = player?.Creature;
        if (duelist == null || duelist.MaxHp <= 0)
            return StatEffectTotalMultiplier.Identity;

        decimal playerPct = (decimal)duelist.CurrentHp / duelist.MaxHp;

        var cs = duelist.CombatState;
        if (cs == null)
            return StatEffectTotalMultiplier.Identity;

        int sumEnemyCurrent = 0;
        int sumEnemyMax = 0;
        foreach (Creature enemy in cs.HittableEnemies.Where(e => e.IsAlive))
        {
            sumEnemyCurrent += enemy.CurrentHp;
            sumEnemyMax += enemy.MaxHp;
        }

        if (sumEnemyMax <= 0)
            return StatEffectTotalMultiplier.MegamorphHalveAtk;

        decimal enemyPct = (decimal)sumEnemyCurrent / sumEnemyMax;
        return playerPct < enemyPct
            ? StatEffectTotalMultiplier.MegamorphDoubleAtk
            : StatEffectTotalMultiplier.MegamorphHalveAtk;
    }
}
