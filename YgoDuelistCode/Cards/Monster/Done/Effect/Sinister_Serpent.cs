using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>After this card was sent to your Graveyard from the field: once on your next turn start while it remains there, add it to your hand.</summary>
public sealed class Sinister_Serpent : EffectMonsterCard
{
    private bool _pendingReturnToHand;

    public Sinister_Serpent()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 3,
            baseDef: 2,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Reptile)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Water | YgoCardPackTags.Ocean;

    public override Type[] RelatedCards => new[] { typeof(Sinister_Serpent) };

    public override void OnAfterPileMoveCompleted(Player? player, PileType? from, PileType newPileType)
    {
        if (player == null || from != MonsterPile.CustomType || newPileType != GraveyardPile.CustomType)
            return;
        _pendingReturnToHand = true;
    }

    public override async Task OnGraveyardRelicOwnerTurnStartWhileInGraveyardAsync(
        PlayerChoiceContext ctx,
        Player player,
        GraveyardRelic relic)
    {
        if (!_pendingReturnToHand)
            return;
        if (!relic.TryConsumeAnnual("SINISTER_SERPENT_HAND"))
            return;
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (gy == null || hand == null || !gy.Cards.Contains(this))
            return;

        _pendingReturnToHand = false;
        await CardPileCmd.Add(new[] { this }, hand, CardPilePosition.Top, this, false);
    }
}
