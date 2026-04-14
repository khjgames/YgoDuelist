using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>When destroyed by battle: optional Special Summon 1 Zombie with printed DEF 20 or less from the deck (<see cref="Services.YgoBattleDeathOptionalDeckSpecialSummon"/>).</summary>
public sealed class Pyramid_Turtle : EffectMonsterCard, IBattleDeathOptionalDeckSpecialSummon
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-PYRAMID_TURTLE.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-PYRAMID_TURTLE.summon_zombie");

    public Pyramid_Turtle()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 14,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathActivatePrompt => ActivatePrompt;

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathSummonPrompt => SummonPrompt;

    bool IBattleDeathOptionalDeckSpecialSummon.IsBattleDeathDeckSummonCandidate(BaseMonsterCard m) =>
        m.DuelMonsterRace == DuelMonsterRace.Zombie && m.BaseDef <= 20 && m.CanSummonDuelMonster;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Zombie;
}
