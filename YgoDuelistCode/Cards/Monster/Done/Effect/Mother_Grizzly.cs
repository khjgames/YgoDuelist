using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Battle-death optional summon — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoGraveyardOptionalDeckSpecialSummon"/>.</summary>
public sealed class Mother_Grizzly : EffectMonsterCard, IBattleDeathOptionalDeckSpecialSummon
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-MOTHER_GRIZZLY.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-MOTHER_GRIZZLY.summon_water");

    public Mother_Grizzly()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 14,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathActivatePrompt => ActivatePrompt;

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathSummonPrompt => SummonPrompt;

    bool IBattleDeathOptionalDeckSpecialSummon.IsBattleDeathDeckSummonCandidate(BaseMonsterCard m) =>
        m.DuelMonsterAttribute == DuelMonsterAttribute.Water && m.BaseAtk <= 15 && m.CanSummonDuelMonster;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water | YgoCardPackTags.Warrior;
    public override Type[] RelatedCards => new[] { typeof(Mother_Grizzly) };
}
