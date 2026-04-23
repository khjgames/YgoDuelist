using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Vanilla calls <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCardPlayQueue.OnActionEnqueued"/> when a
/// <see cref="PlayCardAction"/> is queued, before <see cref="PlayCardAction.ExecuteAction"/>. Any flow that runs
/// cancelable grids or async prep before <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCardPlayQueue.UpdateCardBeforeExecution"/>
/// must skip that early enqueue so the first queue sync matches the real play body (same as vanilla
/// <see cref="PlayCardAction.ExecuteAction"/> line order).
/// <para />
/// MP: use <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetCombatCard.ToCardModelOrNull"/> only — never
/// <see cref="MegaCrit.Sts2.Core.Entities.Multiplayer.NetCombatCard.ToCardModel"/>. Vanilla
/// <c>NCardPlayQueue.OnActionEnqueued</c> already uses OrNull + <c>CardModelId</c> fallback; throwing here breaks
/// observers when <see cref="YgoDuelist.YgoDuelistCode.GameActions.YgoMonsterMenuCommandGameAction"/> (OpenMonsterOptions) is queued ahead of a
/// <see cref="PlayCardAction"/> whose <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.NetCombatCard"/> ids are
/// not registered until that menu action runs.
/// <para />
/// Keep this in sync with Harmony prefixes on <c>PlayCardAction.ExecuteAction</c> that return <c>false</c> and only
/// call <c>UpdateCardBeforeExecution</c> inside their duplicated vanilla body after user confirmation.
/// Spell/trap plays from the field also require matching bypass in <c>PlayCardFromSpellTrapZonePatch</c> (priority 900):
/// implement <see cref="IYgoPrePlayCancelableGridSelection"/> for grid+before-spend flows, ideally through shared
/// helpers such as <see cref="YgoPrePlayGridSelection"/> / <see cref="YgoCardGridChoice"/>, or add the card type there
/// if it uses a dedicated patch (Riryoku, Emergency Provisions, etc.).
/// Option pile: only <see cref="Activate_Effect"/> + <see cref="IMonsterActivatedEffectPrePlaySelection"/> defers
/// queue (see <c>PlayCardFromOptionPilePatch</c>); do not defer for every option-pile card.
/// </summary>
public static class YgoPlayCardQueueDeferral
{
    /// <summary>Same pile rule as <see cref="YgoDuelist.YgoDuelistCode.Patches.PlayCardActionEquipSpellPatch"/>.</summary>
    public static bool IsEquipSpellPlayPile(BaseEquipSpellCard equip, CardPile? pile)
    {
        if (pile?.Type == PileType.Hand)
            return true;
        return pile?.Type == SpellTrapZonePile.CustomType && equip.FaceDown;
    }

    /// <summary>
    /// Returns whether <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCardPlayQueue.OnActionEnqueued"/> should be skipped
    /// so the first queue touch is <see cref="MegaCrit.Sts2.Core.Nodes.Combat.NCardPlayQueue.UpdateCardBeforeExecution"/>
    /// after prep (aligned with custom <see cref="PlayCardAction.ExecuteAction"/> bodies).
    /// </summary>
    /// <param name="reason">Short label for logs (e.g. <c>fusion_spell</c>).</param>
    public static bool ShouldSkipOnActionEnqueued(PlayCardAction action, out string? reason)
    {
        reason = null;

        if (!CombatManager.Instance.IsInProgress)
            return false;

        CardModel? card = action.NetCombatCard.ToCardModelOrNull();
        if (card == null)
        {
            GD.Print(
                $"[YgoDuelist][MP][Queue][Defer] NetCombatCard index={action.NetCombatCard.CombatCardIndex} not in DB yet (menu/open ordering); defer checks skipped, vanilla OnActionEnqueued uses CardModelId fallback");
            return false;
        }

        Player? player = action.Player;
        if (player != null)
        {
            CardPile? optionPile = YgoPlayerPiles.OptionPile(player);
            if (optionPile != null
                && ReferenceEquals(card.Pile, optionPile)
                && card is MonsterCommandCard mcc
                && mcc.TryGetOptionPilePlayCardQueueDeferral(player, out reason))
                return true;
        }

        if (TributeSummonSelection.IsHandTributeDuelNormalSummonPlay(action))
        {
            reason = "hand_tribute_normal_summon";
            return true;
        }

        // Second-hand option row uses YgoCardOptionPile — same defer rules as hand/zone for ritual/fusion/equip prep.
        bool handOrSpellTrapZone = card.Pile?.Type == PileType.Hand
            || card.Pile?.Type == SpellTrapZonePile.CustomType
            || card.Pile?.Type == YgoCardOptionPile.CustomType;
        if (!handOrSpellTrapZone)
            return false;

        if (card is IYgoPrePlayCancelableGridSelection)
        {
            reason = "pre_play_cancelable_grid";
            return true;
        }

        if (card is FusionSpellCard)
        {
            reason = "fusion_spell";
            return true;
        }

        if (card is RitualSpellCard)
        {
            reason = "ritual_spell";
            return true;
        }

        if (card is BaseEquipSpellCard equip && IsEquipSpellPlayPile(equip, card.Pile))
        {
            reason = "equip_spell";
            return true;
        }

        if (card is BaseSpellCard bs && bs.TryGetPlayCardQueueOnActionEnqueuedDeferral(out reason))
            return true;

        return false;
    }

    /// <summary>
    /// <see cref="Patches.PlayCardFromSpellTrapZonePatch"/> must not shortcut these — same cards as hand/zone deferral
    /// that run a custom <see cref="PlayCardAction.ExecuteAction"/> body after prep.
    /// </summary>
    public static bool SpellTrapZonePlayRequiresVanillaExecuteAction(CardModel card)
    {
        if (card is FusionSpellCard
            or RitualSpellCard
            or BaseEquipSpellCard
            or IYgoPrePlayCancelableGridSelection)
            return true;
        return card is BaseSpellCard bs && bs.TryGetPlayCardQueueOnActionEnqueuedDeferral(out _);
    }
}
