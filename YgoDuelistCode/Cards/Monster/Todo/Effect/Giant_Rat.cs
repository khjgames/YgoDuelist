using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Battle-death optional summon — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoGraveyardOptionalDeckSpecialSummon"/>.</summary>
public sealed class Giant_Rat : EffectMonsterCard, IBattleDeathOptionalDeckSpecialSummon
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-GIANT_RAT.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-GIANT_RAT.summon_earth");

    public Giant_Rat()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 14,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathActivatePrompt => ActivatePrompt;

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathSummonPrompt => SummonPrompt;

    bool IBattleDeathOptionalDeckSpecialSummon.IsBattleDeathDeckSummonCandidate(BaseMonsterCard m) =>
        m.DuelMonsterAttribute == DuelMonsterAttribute.Earth && m.BaseAtk <= 15 && m.CanSummonDuelMonster;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Normal;

    public override Type[] RelatedCards => new[] { typeof(Giant_Rat) };
}
