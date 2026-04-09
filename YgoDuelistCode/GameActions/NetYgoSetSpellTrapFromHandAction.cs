using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace YgoDuelist.YgoDuelistCode.GameActions;

/// <summary>
/// Network payload for <see cref="YgoSetSpellTrapFromHandGameAction"/> (set spell/trap from hand into SpellTrapZonePile).
/// Registered with vanilla <c>INetAction</c> discovery (<c>ReflectionHelper.GetSubtypesInMods&lt;INetAction&gt;()</c>).
/// <para>
/// <see cref="NetCombatCard"/> alone is insufficient when <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.NetCombatCardDb"/>
/// assigns different uint indices to the same logical card on host vs client; include <see cref="ModelId"/> and
/// <see cref="SameIdHandOrdinal"/> so the correct hand instance is chosen (mirrors <see cref="MegaCrit.Sts2.Core.GameActions.NetPlayCardAction"/>).
/// </para>
/// </summary>
public struct NetYgoSetSpellTrapFromHandAction : INetAction, IPacketSerializable
{
    public NetCombatCard card;

    public ModelId modelId;

    /// <summary>0-based: count of same-<see cref="ModelId"/> cards before this one in <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardPileType.Hand"/> list order.</summary>
    public byte sameIdHandOrdinal;

    public GameAction ToGameAction(Player player)
    {
        return new YgoSetSpellTrapFromHandGameAction(player, card, modelId, sameIdHandOrdinal);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.Write(card);
        writer.WriteModelEntry(modelId);
        writer.WriteByte(sameIdHandOrdinal);
    }

    public void Deserialize(PacketReader reader)
    {
        card = reader.Read<NetCombatCard>();
        modelId = reader.ReadModelIdAssumingType<CardModel>();
        sameIdHandOrdinal = reader.ReadByte();
    }
}
