using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;

/// <summary>When destroyed by battle: optional Special Summon <see cref="Mirage_Knight"/> from hand or deck — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoGraveyardOptionalDeckSpecialSummon"/>.</summary>
public sealed class Dark_Flare_Knight : FusionMonsterCard, IBattleDeathOptionalDeckSpecialSummon
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-DARK_FLARE_KNIGHT.activate_destroyed_by_battle");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-DARK_FLARE_KNIGHT.summon_mirage_knight");

    public Dark_Flare_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 22,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior,
            typeof(Dark_Magician),
            typeof(Flame_Swordsman))
    {
    }

    public override float PackWeightMultiplier => 1.10f;

    public override Type[] RelatedCards => new[] { typeof(Dark_Flare_Knight), typeof(Mirage_Knight) };

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathActivatePrompt => ActivatePrompt;

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathSummonPrompt => SummonPrompt;

    bool IBattleDeathOptionalDeckSpecialSummon.IsBattleDeathDeckSummonCandidate(BaseMonsterCard m) =>
        m is Mirage_Knight;

    bool IBattleDeathOptionalDeckSpecialSummon.BattleDeathSummonSearchHandAndDeck => true;
}
