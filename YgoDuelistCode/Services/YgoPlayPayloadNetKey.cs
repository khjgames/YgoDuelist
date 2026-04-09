using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// MP-stable identity for pending play payloads: <see cref="NetCombatCard"/> index is shared across peers via
/// <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.NetCombatCardDb"/>; <see cref="CardModel"/> reference equality is not.
/// </summary>
public static class YgoPlayPayloadNetKey
{
    public static bool TryGetKey(CardModel? card, out ulong ownerNetId, out uint combatCardIndex)
    {
        ownerNetId = 0;
        combatCardIndex = 0;
        if (card?.Owner == null)
            return false;

        try
        {
            ownerNetId = card.Owner.NetId;
            combatCardIndex = NetCombatCard.FromModel(card).CombatCardIndex;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
