using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// While face-up on the field, any card that would be sent to the Graveyard is banished instead — see <see cref="YgoBanisherOfLightRules"/> and CardPileCmdBanisherOfLightGyRedirectPatch.
/// </summary>
public sealed class Banisher_of_the_Light : EffectMonsterCard
{
    public Banisher_of_the_Light()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 1,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Banisher_of_the_Light) };
}
