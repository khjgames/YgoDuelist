using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Insect_Princess : EffectMonsterCard
{
    private const int Mgc2Base = 5;

    public Insect_Princess()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 19,
            baseDef: 12,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override Type[] RelatedCards => new[] { typeof(Insect_Princess), typeof(Insect_Queen) };

    public override YgoCardPackTags PackTags => YgoCardPackTags.Wind | YgoCardPackTags.Insect;

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (Owner == null || target.DuelMonsterRace != DuelMonsterRace.Insect)
            return StatEffectTotal.None;

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return new StatEffectTotal(mgc, mgc);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", Mgc2Base) });

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2;
        DynamicVars["Mgc2"].BaseValue = Mgc2Base + 3;
    }
}
