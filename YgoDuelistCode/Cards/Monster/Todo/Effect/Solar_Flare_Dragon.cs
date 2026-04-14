using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>With another Pyro on the field: +<c>Mgc</c> ATK/DEF. End of turn: <c>Mgc2</c> Blight on a random enemy (see <see cref="YgoDuelist.YgoDuelistCode.Services.YgoSolarFlareDragonEndPhase"/>).</summary>
public sealed class Solar_Flare_Dragon : EffectMonsterCard
{
    public Solar_Flare_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 15,
            baseDef: 10,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Fire;

    public override Type[] RelatedCards => new[] { typeof(Solar_Flare_Dragon) };

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in base.CanonicalVars)
                yield return v;
            yield return new DynamicVar("Mgc2", 5m);
        }
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        bool otherPyro = DuelMonsterFieldRegistry
            .GetFieldMonsters(Owner)
            .Any(m => m != null && !m.FaceDown && !ReferenceEquals(m, this) && m.DuelMonsterRace == DuelMonsterRace.Pyro);
        if (!otherPyro)
            return (0, 0);

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (mgc, mgc);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
        DynamicVars["Mgc2"].BaseValue = 7m;
    }
}
