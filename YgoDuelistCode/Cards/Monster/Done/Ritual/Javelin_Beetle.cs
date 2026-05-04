using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Ritual;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;

public sealed class Javelin_Beetle : RitualMonsterCard
{
    public Javelin_Beetle()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 24,
            baseDef: 25,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Ritual | YgoCardPackTags.Earth | YgoCardPackTags.Insect | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Javelin_Beetle_Pact), typeof(Javelin_Beetle) };
}
