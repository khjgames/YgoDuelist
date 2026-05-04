using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Gains Mgc ATK for each monster in your Graveyard.
/// </summary>
public sealed class Shadow_Ghoul : EffectMonsterCard
{
    public Shadow_Ghoul()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 16,
            baseDef: 13,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int gyMonsters = YgoPlayerPiles.GraveyardCards(Owner).Count(c => c is BaseMonsterCard);
        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (gyMonsters * mgc, 0);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
