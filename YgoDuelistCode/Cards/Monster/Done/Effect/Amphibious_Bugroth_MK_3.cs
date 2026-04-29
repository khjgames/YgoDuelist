using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Field;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>While <see cref="Umi"/> or <see cref="A_Legendary_Ocean"/> is face-up on your field, attacks apply Blight (50% of damage as stacks).</summary>
public sealed class Amphibious_Bugroth_MK_3 : EffectMonsterCard
{
    public Amphibious_Bugroth_MK_3()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 15,
            baseDef: 13,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Water | YgoCardPackTags.Machine | YgoCardPackTags.Ocean;

    public override Type[] RelatedCards => new[] { typeof(Amphibious_Bugroth_MK_3), typeof(Umi), typeof(A_Legendary_Ocean) };

    public override bool AttackDealsBlightedDamage =>
        Owner != null && HasUmiLikeFieldSpell(Owner);

    public override bool CardShowsBlightKeyword => AttackDealsBlightedDamage;

    private static bool HasUmiLikeFieldSpell(Player player) =>
        YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<Umi>(player)
        || YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<A_Legendary_Ocean>(player);
}
