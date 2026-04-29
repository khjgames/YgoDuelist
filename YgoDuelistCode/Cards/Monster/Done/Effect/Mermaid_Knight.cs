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

public sealed class Mermaid_Knight : EffectMonsterCard
{
    public Mermaid_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 15,
            baseDef: 7,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Water | YgoCardPackTags.Ocean;

    public override Type[] RelatedCards => new[] { typeof(Mermaid_Knight), typeof(Umi), typeof(A_Legendary_Ocean) };

    protected override int GetAttackDefendResolutionCount(Player? player)
    {
        int n = base.GetAttackDefendResolutionCount(player);
        return IsUmiActive(player) ? n + 1 : n;
    }

    private static bool IsUmiActive(Player? player) =>
        YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<Umi>(player)
        || YgoFieldSpellStatAggregator.HasActiveFaceUpFieldSpell<A_Legendary_Ocean>(player);
}
