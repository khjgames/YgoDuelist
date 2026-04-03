using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class Legendary_Flame_Lord : RitualMonsterCard
{
    public Legendary_Flame_Lord()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 24,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Ritual | YgoCardPackTags.Fire | YgoCardPackTags.Spellcaster | YgoCardPackTags.Spell;

    public override Type[] BundledCards => new[] { typeof(Incandescent_Ordeal), typeof(Legendary_Flame_Lord) };

    public override Type[] RelatedCards =>
        RitualArchetypeMeta.RelatedCardsForPairedRitual(typeof(Incandescent_Ordeal), typeof(Legendary_Flame_Lord));
}
