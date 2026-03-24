using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Refreshes spell/trap row <see cref="NCard"/> instances so equip-link portrait overlays update on duel monster hover.
/// Zone cards use a custom pile type; vanilla <see cref="NCard.FindOnTable"/> does not resolve them — they live on <see cref="NPlayerHand"/> as option holders.
/// </summary>
public static class YgoEquipPortraitOverlaySync
{
    private static readonly System.Reflection.MethodInfo? NCardReload =
        AccessTools.Method(typeof(NCard), "Reload", Type.EmptyTypes);

    public static void RefreshSpellTrapRowEquipOverlays(Player? player)
    {
        if (player == null || NCardReload == null)
            return;

        foreach (CardModel c in YgoSpellTrapZoneBridge.GetVisibleCards(player))
        {
            if (c is not BaseEquipSpellCard)
                continue;
            NCard? n = NPlayerHand.Instance?.GetCard(c);
            if (n != null && GodotObject.IsInstanceValid(n))
                NCardReload.Invoke(n, null);
        }
    }
}
