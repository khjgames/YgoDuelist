using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Banish + end-phase return — <see cref="YgoDdScoutPlaneEndPhase"/>.</summary>
public sealed class D_D_Scout_Plane : EffectMonsterCard, IYgoDdScoutPlaneCard, IYgoOwnerBeforeTurnEndFlushBanishedEffect
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-D_D_SCOUT_PLANE.activate_return");
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
        YgoCardPackTags.Dark | YgoCardPackTags.Machine | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(D_D_Scout_Plane) };

    public bool DdScoutEndPhaseUsedThisTurn { get; set; }

    public void MarkBanishedThisOwnerTurn(int stamp) => _banishedThisOwnerTurnStamp = stamp;

    public bool IsBanishedThisTurnForEndPhase(int ownerTurnStamp) =>
        _banishedThisOwnerTurnStamp >= 0 && _banishedThisOwnerTurnStamp == ownerTurnStamp;

    public override void OnAfterPileMoveCompleted(Player? player, PileType? from, PileType newPileType)
    {
        if (player != null && newPileType == BanishedPile.CustomType)
            MarkBanishedThisOwnerTurn(YgoPlayerCombatTurnStamp.Get(player));
    }

    public bool IsOwnerBeforeTurnEndFlushBanishedEffectActive() =>
        Owner != null && IsBanishedThisTurnForEndPhase(YgoPlayerCombatTurnStamp.Get(Owner));

    public async Task TryResolveOwnerBeforeTurnEndFlushBanishedEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        CardPile? banished = YgoPlayerPiles.Banished(owner);
        if (banished == null || !banished.Cards.Contains(this))
            return;
        int stamp = YgoPlayerCombatTurnStamp.Get(owner);
        if (!IsBanishedThisTurnForEndPhase(stamp) || DdScoutEndPhaseUsedThisTurn)
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(owner, 0))
            return;
        var prefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };
        D_D_Scout_Plane? pick = await YgoOrderedCardSelection.TryConfirmSingleCardAsync(
            choiceContext,
            owner,
            prefs,
            this);
        if (!ReferenceEquals(pick, this))
            return;
        if (!banished.Cards.Contains(this))
            return;
        DdScoutEndPhaseUsedThisTurn = true;
        await CardPileCmd.RemoveFromCombat(this, false);
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(owner, this, choiceContext);
    }
}
