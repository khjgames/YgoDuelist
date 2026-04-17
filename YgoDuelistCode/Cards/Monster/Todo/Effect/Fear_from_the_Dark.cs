using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>
/// Effect monster with printed ATK/DEF. When sent from hand or draw pile to the Graveyard, Special Summons itself.
/// </summary>
public sealed class Fear_from_the_Dark : EffectMonsterCard, IYgoAfterMonsterMovedToGraveyardFromHandOrDraw
{
    public Fear_from_the_Dark()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 17,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Zombie;

    public override Type[] RelatedCards => new[]
    {
        typeof(Fear_from_the_Dark),
    };

    public async Task OnAfterMovedToGraveyardFromHandOrDrawAsync(Player player, PileType fromPile)
    {
        _ = fromPile;
        if (CombatManager.Instance is not { IsInProgress: true })
            return;
        if (player.Creature?.CombatState == null || player.Creature.Side != CombatSide.Player)
            return;
        if (!GraveyardRelic.GetGraveyardCards(player).Contains(this))
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, tributeReleaseCount: 0))
            return;
        var ctx = new BlockingPlayerChoiceContext();
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, ctx);
    }
}
