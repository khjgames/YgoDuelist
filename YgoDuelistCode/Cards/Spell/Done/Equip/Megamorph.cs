using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Megamorph : BaseEquipSpellCard
{
    public Megamorph()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Spell;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Megamorph),
    };

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
        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
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
