using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Banish + end-phase return — <see cref="YgoDdScoutPlaneEndPhase"/>.</summary>
public sealed class D_D_Scout_Plane : EffectMonsterCard
{
    private int _banishedThisOwnerTurnStamp = -1;

    public D_D_Scout_Plane()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 8,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Machine | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(D_D_Scout_Plane) };

    public bool DdScoutEndPhaseUsedThisTurn { get; set; }

    internal void MarkBanishedThisOwnerTurn(int stamp) => _banishedThisOwnerTurnStamp = stamp;

    internal bool IsBanishedThisTurnForEndPhase(int ownerTurnStamp) =>
        _banishedThisOwnerTurnStamp >= 0 && _banishedThisOwnerTurnStamp == ownerTurnStamp;
}
