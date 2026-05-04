using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Battle-death optional summon — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoGraveyardOptionalDeckSpecialSummon"/>.</summary>
public sealed class Shining_Angel : EffectMonsterCard, IBattleDeathOptionalDeckSpecialSummon
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-SHINING_ANGEL.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-SHINING_ANGEL.summon_light");

    public Shining_Angel()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 14,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathActivatePrompt => ActivatePrompt;

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathSummonPrompt => SummonPrompt;

    bool IBattleDeathOptionalDeckSpecialSummon.IsBattleDeathDeckSummonCandidate(BaseMonsterCard m) =>
        m.DuelMonsterAttribute == DuelMonsterAttribute.Light && m.BaseAtk <= 15 && m.CanSummonDuelMonster;

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Normal;

    public override Type[] RelatedCards => new[] { typeof(Shining_Angel) };
}
