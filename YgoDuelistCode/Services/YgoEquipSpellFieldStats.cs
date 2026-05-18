using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Shared face-up zone / field counts for equip spells (Mage Power, United We Stand, etc.).</summary>
public static class YgoEquipSpellFieldStats
{
    public static IReadOnlyList<BaseEquipSpellCard> GetOwnerFaceUpEquipsInZone(Player? owner)
    {
        if (owner == null)
            return System.Array.Empty<BaseEquipSpellCard>();

        CardPile? zone = YgoPlayerPiles.SpellTrapZone(owner);
        if (zone == null)
            return System.Array.Empty<BaseEquipSpellCard>();

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(zone.Cards)
            .OfType<BaseEquipSpellCard>()
            .Where(e => !e.FaceDown)
            .ToArray();
    }

    public static int CountOwnerFaceUpFieldMonsters(Player? owner)
    {
        if (owner == null)
            return 0;

        return DuelMonsterFieldRegistry.OrderedFieldMonsters(owner).Count(m => !m.FaceDown);
    }

    public static int CountOwnerFaceUpSpellTraps(Player? owner)
    {
        CardPile? zone = YgoPlayerPiles.SpellTrapZone(owner);
        if (zone == null)
            return 0;

        return zone.Cards.Count(c => c switch
        {
            BaseSpellCard { FaceDown: true } => false,
            BaseTrapCard { FaceDown: true } => false,
            _ => true
        });
    }
}
