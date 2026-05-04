using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>While you control another Fiend monster, this card gains {Mgc} ATK and DEF.</summary>
public sealed class Twin_Headed_Wolf : EffectMonsterCard
{
    public override int AttackPortionCount => 2;

    public Twin_Headed_Wolf()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 15,
            baseDef: 10,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Twin_Headed_Wolf) };

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        bool otherFiend = DuelMonsterFieldRegistry
            .OrderedFieldMonsters(Owner)
            .Any(m => m != null && !ReferenceEquals(m, this) && !m.FaceDown && m.DuelMonsterRace == DuelMonsterRace.Fiend);
        if (!otherFiend)
            return (0, 0);

        int b = (int)DynamicVars["Mgc"].BaseValue;
        return (b, b);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
    }
}
