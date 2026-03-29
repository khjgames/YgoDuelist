using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Zone equip spell or spell/trap equip-link card selected or hovered in hand/second-hand (matches <see cref="HoveredModelTracker"/> priority).
/// Used to draw equip-link overlay on the attached duel monster portrait.
/// </summary>
public static class YgoZoneEquipHandHoverState
{
    public static CardModel? HoveredOrSelectedZoneEquipLink { get; private set; }

    public static void RecomputeFromTracker(HoveredModelTracker tracker)
    {
        var sel = AccessTools.Field(typeof(HoveredModelTracker), "_localSelectedCard")?.GetValue(tracker) as CardModel;
        var hov = AccessTools.Field(typeof(HoveredModelTracker), "_localHoveredCard")?.GetValue(tracker) as CardModel;
        var card = sel ?? hov;

        if (card is BaseEquipSpellCard eq && YgoSpellTrapZoneBridge.IsInZone(eq))
            HoveredOrSelectedZoneEquipLink = eq;
        else if (card is IYgoSpellTrapEquipLink && YgoSpellTrapZoneBridge.IsInZone(card))
            HoveredOrSelectedZoneEquipLink = card;
        else
            HoveredOrSelectedZoneEquipLink = null;

        DuelMonsterPortraitDecorations.RefreshAllEquipLinkLayers();
        YgoEquipPortraitOverlaySync.RefreshSpellTrapRowEquipOverlays(card?.Owner);
    }
}
