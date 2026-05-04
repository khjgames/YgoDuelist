using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class The_Legendary_Fisherman : EffectMonsterCard
{
    public The_Legendary_Fisherman()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 18,
            baseDef: 16,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Water | YgoCardPackTags.Ocean | YgoCardPackTags.Warrior;

    public override Type[] RelatedCards => new[] { typeof(The_Legendary_Fisherman), typeof(Umi), typeof(A_Legendary_Ocean) };

    public override int GetDuelMonsterPlayEnergyDiscount() =>
        (!IsCanonical && Owner != null && YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<Umi>(Owner) ? 1 : 0)
        + GetCostDownHandPlayEnergyDiscount();
}
