using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Zone equip spell that is currently selected or hovered in hand/second-hand (matches <see cref="HoveredModelTracker"/> priority).
/// Used to draw equip-link overlay on the attached duel monster portrait.
/// </summary>
public static class YgoZoneEquipHandHoverState
{
    public static BaseEquipSpellCard? HoveredOrSelectedZoneEquip { get; private set; }

    public static void RecomputeFromTracker(HoveredModelTracker tracker)
    {
        var sel = AccessTools.Field(typeof(HoveredModelTracker), "_localSelectedCard")?.GetValue(tracker) as CardModel;
        var hov = AccessTools.Field(typeof(HoveredModelTracker), "_localHoveredCard")?.GetValue(tracker) as CardModel;
        var card = sel ?? hov;

        if (card is BaseEquipSpellCard eq && YgoSpellTrapZoneBridge.IsInZone(eq))
            HoveredOrSelectedZoneEquip = eq;
        else
            HoveredOrSelectedZoneEquip = null;

        DuelMonsterPortraitDecorations.RefreshAllEquipLinkLayers();
        YgoEquipPortraitOverlaySync.RefreshSpellTrapRowEquipOverlays(card?.Owner);
    }
}
