using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.GameActions;

/// <summary>
/// Synchronized spell/trap "set from hand" into the logical Spell/Trap zone. Replaces UI-only
/// <see cref="YgoSpellTrapZoneBridge.TrySetFromHandAsync"/> calls so host and client both execute the same pile move.
/// </summary>
public sealed class YgoSetSpellTrapFromHandGameAction : GameAction
{
    /// <summary>Ordinal among hand cards with the same <see cref="CardModel.Id"/> (list order), for MP when <see cref="NetCombatCard"/> indices diverge.</summary>
    public static byte ComputeSameIdHandOrdinal(CardModel card, Player player)
    {
        if (player.PlayerCombatState?.Hand == null)
            return 0;

        int o = 0;
        foreach (CardModel c in player.PlayerCombatState.Hand.Cards)
        {
            if (c.Id != card.Id)
                continue;
            if (ReferenceEquals(c, card))
                return (byte)Math.Min(o, 255);
            o++;
        }

        return 0;
    }

    private readonly Player _player;
    private readonly NetCombatCard _netCard;
    private readonly ModelId _modelId;
    private readonly byte _sameIdHandOrdinal;

    public YgoSetSpellTrapFromHandGameAction(Player player, NetCombatCard netCard, ModelId modelId, byte sameIdHandOrdinal)
    {
        _player = player;
        _netCard = netCard;
        _modelId = modelId;
        _sameIdHandOrdinal = sameIdHandOrdinal;
    }

    public override ulong OwnerId => _player.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    protected override async Task ExecuteAction()
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return;

        CardModel? card = ResolveHandCardForSet();
        if (card == null)
        {
            GD.PrintErr(
                $"[YgoDuelist][MP] YgoSetSpellTrapFromHandGameAction: could not resolve card modelId={_modelId.Entry} ordinal={_sameIdHandOrdinal} netIndex={_netCard.CombatCardIndex}");
            return;
        }

        if (card.Pile?.Type != PileType.Hand)
            return;
        if (card.Owner?.NetId != _player.NetId)
            return;

        // Do not check BaseSpellCard.IsSetModeInHand: that flag is local UI (toggle on the enqueuing peer only).
        // Net action is authoritative that this card was set from hand.
        bool canSetFromHand = card is BaseTrapCard || card is BaseSpellCard;
        if (!canSetFromHand)
            return;
        if (!YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(_player, card))
            return;

        GD.Print(
            $"[YgoDuelist][MP] YgoSetSpellTrapFromHandGameAction: TrySetFromHandAsync card={card.Id?.Entry} owner={_player.NetId} netIndex={_netCard.CombatCardIndex}");
        await YgoSpellTrapZoneBridge.TrySetFromHandAsync(card);
    }

    /// <summary>
    /// Prefer <see cref="NetCombatCard.ToCardModelOrNull"/> when it matches the serialized model id; otherwise pick the
    /// ordinal-th matching card in hand (cross-peer NetCombatCard index mismatch).
    /// </summary>
    private CardModel? ResolveHandCardForSet()
    {
        CardModel? byNet = _netCard.ToCardModelOrNull();
        if (byNet != null
            && byNet.Id == _modelId
            && byNet.Pile?.Type == PileType.Hand
            && byNet.Owner?.NetId == _player.NetId)
            return byNet;

        if (byNet != null && (byNet.Id != _modelId || byNet.Pile?.Type != PileType.Hand))
            GD.Print(
                $"[YgoDuelist][MP] YgoSetSpellTrapFromHandGameAction: NetCombatCard {_netCard.CombatCardIndex} -> {byNet.Id?.Entry} (pile={byNet.Pile?.Type}); resolving by modelId+ordinal expected={_modelId.Entry}");

        CardPile? hand = _player.PlayerCombatState?.Hand;
        if (hand == null)
            return null;

        int o = 0;
        foreach (CardModel c in hand.Cards)
        {
            if (c.Id != _modelId)
                continue;
            if (o == _sameIdHandOrdinal)
                return c;
            o++;
        }

        return null;
    }

    public override INetAction ToNetAction()
    {
        return new NetYgoSetSpellTrapFromHandAction
        {
            card = _netCard,
            modelId = _modelId,
            sameIdHandOrdinal = _sameIdHandOrdinal
        };
    }

    public override string ToString()
    {
        return $"YgoSetSpellTrapFromHandGameAction owner={_player.NetId} netCard={_netCard} model={_modelId.Entry} ord={_sameIdHandOrdinal}";
    }
}
