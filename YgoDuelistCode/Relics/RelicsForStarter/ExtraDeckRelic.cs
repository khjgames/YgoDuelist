using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Relics;

public sealed class ExtraDeckRelic : YgoDuelistRelic
{
    public override string PackedIconPath => "relics/extra_deck.png".ImagePath();
    protected override string PackedIconOutlinePath => "relics/relic_outline.png".ImagePath();
    protected override string BigIconPath => "relics/big/extra_deck.png".ImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetExtraDeckCountForOwner();

    private CardPile? _subscribedPile;

    public static void NotifyRunExtraDeckChanged(Player? player)
    {
        if (player == null)
            return;
        foreach (ExtraDeckRelic ed in YgoPlayerRelicAccess.GetRelics<ExtraDeckRelic>(player))
        {
            ed.InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeCombatStart()
    {
        UnsubscribeFromExtraDeckPile();
        SubscribeToCombatExtraDeckPile();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UnsubscribeFromExtraDeckPile();
        SubscribeToRunExtraDeckPile();
        return Task.CompletedTask;
    }

    public static CardPile? GetCombatExtraDeckPile(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return null;
        return CustomPiles.GetCustomPile(player.PlayerCombatState, ExtraDeckPile.CustomType);
    }

    private int GetExtraDeckCountForOwner()
    {
        Player? player = Owner;
        if (player == null)
            return 0;

        if (CombatManager.Instance?.IsInProgress == true)
        {
            CardPile? combat = GetCombatExtraDeckPile(player);
            return combat?.Cards.Count ?? 0;
        }

        return YgoPlayerRunPiles.RunExtraDeck(player)?.Cards.Count ?? 0;
    }

    private void SubscribeToCombatExtraDeckPile()
    {
        Player? player = Owner;
        if (player == null)
            return;

        CardPile? pile = GetCombatExtraDeckPile(player);
        if (pile == null)
            return;

        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnExtraDeckContentsChanged;
        InvokeDisplayAmountChanged();
    }

    private void SubscribeToRunExtraDeckPile()
    {
        Player? player = Owner;
        if (player == null)
            return;

        CardPile? pile = YgoPlayerRunPiles.RunExtraDeck(player);
        if (pile == null)
            return;
        _subscribedPile = pile;
        _subscribedPile.ContentsChanged += OnExtraDeckContentsChanged;
        InvokeDisplayAmountChanged();
    }

    private void UnsubscribeFromExtraDeckPile()
    {
        if (_subscribedPile == null)
            return;

        _subscribedPile.ContentsChanged -= OnExtraDeckContentsChanged;
        _subscribedPile = null;
        InvokeDisplayAmountChanged();
    }

    private void OnExtraDeckContentsChanged()
    {
        InvokeDisplayAmountChanged();
    }

    public static IReadOnlyList<CardModel> GetExtraDeckCards(Player? player)
    {
        if (player == null)
            return [];

        if (CombatManager.Instance?.IsInProgress == true)
        {
            CardPile? combat = GetCombatExtraDeckPile(player);
            if (combat == null)
                return [];
            return combat.Cards.ToList();
        }

        return YgoPlayerRunPiles.RunExtraDeckCards(player);
    }

    public static bool IsExtraDeckRelic(RelicModel? model) => model is ExtraDeckRelic;

    public static ExtraDeckRelic? AsExtraDeck(RelicModel? model) => model as ExtraDeckRelic;
}
