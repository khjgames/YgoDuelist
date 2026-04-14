using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>End-phase GY revive — <see cref="YgoTwinHeadedBehemothEndPhase"/>; field→GY tracking — <see cref="Patches.CardPileCmdFieldMonsterGraveyardEffectsPatch"/>.</summary>
public sealed class Twin_Headed_Behemoth : EffectMonsterCard
{
    private int _gyReviveEligibleStamp = -1;

    public Twin_Headed_Behemoth()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 15,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Dragon;

    public override Type[] RelatedCards => new[] { typeof(Twin_Headed_Behemoth) };

    internal void MarkEndPhaseReviveEligible(int ownerTurnStamp) => _gyReviveEligibleStamp = ownerTurnStamp;

    internal bool IsEndPhaseReviveEligible(int ownerTurnStamp) =>
        _gyReviveEligibleStamp >= 0 && _gyReviveEligibleStamp == ownerTurnStamp;

    internal void ClearEndPhaseReviveEligibility() => _gyReviveEligibleStamp = -1;

    internal void ArmReviveSummonMiniStats()
    {
        if (DynamicVars?.Damage != null)
            DynamicVars.Damage.BaseValue = 10;
        if (DynamicVars != null && DynamicVars.ContainsKey("Def"))
            DynamicVars["Def"].BaseValue = 10;
    }

    internal void ClearReviveMiniStats()
    {
        if (DynamicVars?.Damage != null)
            DynamicVars.Damage.BaseValue = BaseAtk;
        if (DynamicVars != null && DynamicVars.ContainsKey("Def"))
            DynamicVars["Def"].BaseValue = BaseDef;
    }
}
