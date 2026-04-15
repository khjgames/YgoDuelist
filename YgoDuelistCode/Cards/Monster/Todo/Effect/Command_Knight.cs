using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>All your Warriors gain +<c>Mgc</c> ATK. With another monster on your field, this card gets +<c>Mgc2</c> max HP.</summary>
public sealed class Command_Knight : EffectMonsterCard
{
    public Command_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 12,
            baseDef: 19,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Fire | YgoCardPackTags.Warrior;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in base.CanonicalVars)
                yield return v;
            yield return new DynamicVar("Mgc2", 1m);
        }
    }

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (Owner == null || target.Owner != Owner)
            return StatEffectTotal.None;
        if (target.DuelMonsterRace != DuelMonsterRace.Warrior)
            return StatEffectTotal.None;

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return new StatEffectTotal(mgc, 0);
    }

    public override int GetFortifiedBeastsBonusMaxHp(Player player)
    {
        IReadOnlyCollection<BaseMonsterCard>? field = DuelMonsterFieldRegistry.GetFieldMonsters(player);
        if (field == null || field.Count < 2)
            return 0;
        return (int)DynamicVars["Mgc2"].BaseValue;
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 6m;
        DynamicVars["Mgc2"].BaseValue = 2m;
    }
}
