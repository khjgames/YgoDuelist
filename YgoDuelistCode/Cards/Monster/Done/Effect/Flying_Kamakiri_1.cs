using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Graveyard optional summon — <see cref="YgoGraveyardOptionalDeckSpecialSummon"/>.</summary>
public sealed class Flying_Kamakiri_1 : EffectMonsterCard, IGraveyardOptionalDeckSpecialSummon
{
    public override int AttackPortionCount => 2;
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-FLYING_KAMAKIRI_1.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-FLYING_KAMAKIRI_1.summon_wind");

    public Flying_Kamakiri_1()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 14,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    LocString IGraveyardOptionalDeckSpecialSummon.GraveyardActivatePrompt => ActivatePrompt;

    LocString IGraveyardOptionalDeckSpecialSummon.GraveyardSummonPrompt => SummonPrompt;

    bool IGraveyardOptionalDeckSpecialSummon.IsGraveyardDeckSummonCandidate(BaseMonsterCard m) =>
        m.DuelMonsterAttribute == DuelMonsterAttribute.Wind && m.BaseAtk <= 15 && m.CanSummonDuelMonster;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Insect;

    public override Type[] RelatedCards => new[] { typeof(Flying_Kamakiri_1), typeof(Flying_Kamakiri_2) };
}
