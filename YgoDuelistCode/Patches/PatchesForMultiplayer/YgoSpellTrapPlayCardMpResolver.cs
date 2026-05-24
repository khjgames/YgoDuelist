using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// MP <see cref="NetCombatCard"/> indices can point at a different <see cref="CardModel"/> instance with the same
/// <see cref="ModelId"/> (e.g. one UNLEASH in hand vs one set in the spell/trap zone). Vanilla
/// <see cref="NCardPlayQueue.OnActionEnqueued"/> and <see cref="NCardPlayQueue.UpdateCardBeforeExecution"/> always use
/// <c>NetCombatCard.ToCardModel()</c>, so the play-queue <see cref="MegaCrit.Sts2.Core.Nodes.Cards.NCard"/> is wired to
/// the wrong instance while custom spell/trap execution runs <see cref="CardModel.OnPlayWrapper"/> on the zone card —
/// queue removal / tweens then throw or desync checksums.
/// </summary>
public static class YgoSpellTrapPlayCardMpResolver
{
    private static readonly FieldInfo? NetCombatCardBacking = FindNetCombatCardBackingField();

    private static FieldInfo? FindNetCombatCardBackingField()
    {
        var t = typeof(PlayCardAction);
        var f = AccessTools.Field(t, "<NetCombatCard>k__BackingField");
        if (f != null)
            return f;
        foreach (var fi in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            if (fi.FieldType == typeof(NetCombatCard))
                return fi;
        }

        return null;
    }

    /// <summary>
    /// Prefer the spell/trap zone instance matching <see cref="PlayCardAction.CardModelId"/> when the net index
    /// resolves to another pile's copy (face-down set / field spell only — not face-up equips or active continuous).
    /// </summary>
    public static CardModel? ResolveSpellTrapPlayCard(PlayCardAction action)
    {
        Player? player = action.Player;
        if (player == null)
            return null;

        CardPile? zonePile = YgoPlayerPiles.SpellTrapZone(player);
        ModelId expectedId = action.CardModelId;

        CardModel? byIndex = action.NetCombatCard.ToCardModelOrNull();

        // No spell/trap zone copy of this model id — NetCombatCard cannot refer to a "wrong pile" duplicate for it.
        // Avoids misleading "SpellTrap play" logs and extra work on every normal hand play (e.g. duplicate Strikes).
        if (zonePile != null && !zonePile.Cards.Any(c => c.Id == expectedId))
            return byIndex;
        if (byIndex != null && byIndex.Id == expectedId && zonePile != null && ReferenceEquals(byIndex.Pile, zonePile))
            return byIndex;

        if (byIndex != null && byIndex.Id == expectedId && zonePile != null && !ReferenceEquals(byIndex.Pile, zonePile))
            GD.Print(
                $"[YgoDuelist][MP] SpellTrap play: net id {action.NetCombatCard.CombatCardIndex} -> {byIndex.Pile?.Type}, using zone card by model (MP id order)");
        else if (byIndex != null && byIndex.Id != expectedId)
            GD.PrintErr(
                $"[YgoDuelist][MP] SpellTrap play: net id {action.NetCombatCard.CombatCardIndex} -> {byIndex.Id?.Entry} != payload {expectedId.Entry}; resolving by model in zone");

        if (zonePile == null)
            return byIndex;

        CardModel? zoneMatch = null;
        foreach (CardModel c in zonePile.Cards)
        {
            if (c.Id != expectedId)
                continue;
            // Face-up non-field zone copies are already active (equipped/continuous). A hand play must keep
            // the hand instance — rebinding to the field copy blocks equip-from-hand (see Burning Spear logs).
            if (!IsFaceDownOrFieldSpellInZone(c))
                continue;
            if (zoneMatch != null)
            {
                GD.PrintErr(
                    $"[YgoDuelist][MP] SpellTrap play: multiple {expectedId.Entry} in zone; using first (pile order)");
                break;
            }

            zoneMatch = c;
        }

        return zoneMatch ?? byIndex;
    }

    /// <summary>
    /// When the authoritative instance lives in the spell/trap zone but <see cref="NetCombatCard"/> still maps to
    /// another pile's copy, reassign the struct so queue + execution agree.
    /// </summary>
    public static bool TryRebindNetCombatCardIfSpellTrapZone(PlayCardAction action)
    {
        if (NetCombatCardBacking == null)
        {
            GD.PrintErr("[YgoDuelist][MP] TryRebindNetCombatCardIfSpellTrapZone: could not find PlayCardAction NetCombatCard field");
            return false;
        }

        CardModel? resolved = ResolveSpellTrapPlayCard(action);
        CardPile? zonePile = YgoPlayerPiles.SpellTrapZone(action.Player);
        if (resolved == null || zonePile == null || !ReferenceEquals(resolved.Pile, zonePile))
            return false;

        CardModel? fromNet = action.NetCombatCard.ToCardModelOrNull();
        if (ReferenceEquals(fromNet, resolved))
            return false;

        uint oldIndex = action.NetCombatCard.CombatCardIndex;
        PileType? oldPile = fromNet?.Pile?.Type;
        var newNet = NetCombatCard.FromModel(resolved);
        NetCombatCardBacking.SetValue(action, newNet);
        GD.Print(
            $"[YgoDuelist][MP] PlayCardAction NetCombatCard rebound {action.CardModelId.Entry}: index {oldIndex} (pile {oldPile}) -> index {newNet.CombatCardIndex} (spell/trap zone instance)");
        return true;
    }

    private static bool IsFaceDownOrFieldSpellInZone(CardModel card) =>
        YgoSpellTrapZoneBridge.IsFieldSpell(card)
        || card is BaseTrapCard { FaceDown: true }
        || card is BaseSpellCard { FaceDown: true };
}
