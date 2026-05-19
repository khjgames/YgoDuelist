using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Cannot be Special Summoned. This card's attacks hit all enemies.</summary>
public sealed class Asura_Priest : SpiritEffectMonsterCard
{
    public Asura_Priest()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 17,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Light | YgoCardPackTags.Spellcaster;

    public override Type[] RelatedCards => new[] { typeof(Asura_Priest) };

    public override bool BlocksSpecialDuelMonsterSummon => true;

    public override bool DuelMonsterAttackHitsAllEnemies => true;
}
