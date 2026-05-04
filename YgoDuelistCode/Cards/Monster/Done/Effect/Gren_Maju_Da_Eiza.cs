using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>ATK/DEF equal the number of banished cards from all players multiplied by {Mgc}.</summary>
public sealed class Gren_Maju_Da_Eiza : EffectMonsterCard
{
    public Gren_Maju_Da_Eiza()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fire | YgoCardPackTags.Fiend | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Gren_Maju_Da_Eiza) };

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        int banishedCount = 0;
        if (Owner.Creature?.CombatState is { } cs)
        {
            foreach (var player in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
            {
                CardPile? banished = YgoPlayerPiles.Banished(player);
                if (banished == null)
                    continue;
                banishedCount += YgoMpCombatOrder.CardsSnapshotOrderedForMp(banished.Cards).Count;
            }
        }
        else
        {
            CardPile? ownBanished = YgoPlayerPiles.Banished(Owner);
            if (ownBanished != null)
                banishedCount = ownBanished.Cards.Count;
        }

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        int stat = Math.Clamp(banishedCount * mgc, 0, 9999);
        return (stat, stat);
    }
}
