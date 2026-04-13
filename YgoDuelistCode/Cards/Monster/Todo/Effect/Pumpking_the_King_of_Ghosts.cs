using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>
/// Gains Mgc ATK and Mgc DEF while <see cref="Castle_of_Dark_Illusions"/> is face-up on your field.
/// </summary>
public sealed class Pumpking_the_King_of_Ghosts : EffectMonsterCard
{
    public Pumpking_the_King_of_Ghosts()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 18,
            baseDef: 20,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        bool castleUp = DuelMonsterFieldRegistry.GetFieldMonsters(Owner)
            .Any(m => m is Castle_of_Dark_Illusions && !m.FaceDown);
        if (!castleUp)
            return base.GetSecondaryStats();

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (mgc, mgc);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
