using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Buster_Rancher : BaseEquipSpellCard
{
    private const int PrintedBonusAtk = 25;
    private const int PrintedMaxBaseAtk = 10;
    private const int PrintedIntentThreshold = 25;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", (decimal)PrintedBonusAtk),
            new DynamicVar("Mgc2", (decimal)PrintedMaxBaseAtk),
            new DynamicVar("Mgc3", (decimal)PrintedIntentThreshold)
        };

    public Buster_Rancher()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override bool CanEquipTo(BaseMonsterCard target)
    {
        int maxBase = (int)DynamicVars["Mgc2"].BaseValue;
        return target.BaseAtk <= maxBase;
    }

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped)
    {
        if (!EnemyMeetsIntentThreshold(equipped.Owner))
            return StatEffectTotal.None;

        return new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);
    }

    public override int GetEquipAttackPlayEnergyDiscount(BaseMonsterCard equipped) => -1;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = 30m;
        DynamicVars["Mgc2"].BaseValue = 15m;
        DynamicVars["Mgc3"].BaseValue = 18m;
    }

    private bool EnemyMeetsIntentThreshold(Player? owner)
    {
        Creature? pc = owner?.Creature;
        CombatState? cs = pc?.CombatState;
        if (cs == null || pc == null)
            return false;

        int threshold = (int)DynamicVars["Mgc3"].BaseValue;
        return YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs)
            .Any(e => YgoIntentAttackDamage.GetTotalAttackIntentDamage(e, pc) >= threshold);
    }
}
