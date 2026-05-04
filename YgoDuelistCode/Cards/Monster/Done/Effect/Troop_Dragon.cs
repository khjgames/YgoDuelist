using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Destroyed by battle: optional Special Summon 1 Troop Dragon from the deck — <see cref="YgoGraveyardOptionalDeckSpecialSummon"/> pattern.</summary>
public sealed class Troop_Dragon : EffectMonsterCard, IBattleDeathOptionalDeckSpecialSummon
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-TROOP_DRAGON.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-TROOP_DRAGON.summon_dragon");

    public Troop_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 7,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathActivatePrompt => ActivatePrompt;

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathSummonPrompt => SummonPrompt;

    bool IBattleDeathOptionalDeckSpecialSummon.IsBattleDeathDeckSummonCandidate(BaseMonsterCard m) =>
        m is Troop_Dragon && m.CanSummonDuelMonster;

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Dragon;

    public override Type[] RelatedCards => new[] { typeof(Troop_Dragon) };
}
