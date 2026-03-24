using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Refreshes spell/trap row <see cref="NCard"/> instances so equip-link portrait overlays update on duel monster hover.
/// </summary>
public static class YgoEquipPortraitOverlaySync
{
    private static readonly System.Reflection.MethodInfo? NCardReload =
        AccessTools.Method(typeof(NCard), "Reload", Type.EmptyTypes);

    public static void RefreshSpellTrapRowEquipOverlays(Player? player)
    {
        if (player == null || NCardReload == null)
            return;

        if (YgoSecondHandSourceBridge.GetSource(player) != YgoSecondHandSource.SpellTrapZone)
            return;

        foreach (CardModel c in YgoSpellTrapZoneBridge.GetVisibleCards(player))
        {
            if (c is not BaseEquipSpellCard)
                continue;
            NCard? n = NCard.FindOnTable(c);
            if (n != null && GodotObject.IsInstanceValid(n))
                NCardReload.Invoke(n, null);
        }
    }
}
