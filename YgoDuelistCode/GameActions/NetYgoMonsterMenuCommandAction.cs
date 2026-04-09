using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace YgoDuelist.YgoDuelistCode.GameActions;

/// <summary>Wire kind for <see cref="YgoMonsterMenuCommandGameAction"/> (duel monster option row, unplayable menu cards).</summary>
/// <remarks>Underlying type must be <see cref="int"/> — vanilla <see cref="PacketWriter.WriteEnum{T}"/> rejects <c>byte</c>-backed enums.</remarks>
public enum YgoMonsterMenuCommandKind
{
    ToggleDieForYou = 0,
    ChangeBattlePosition = 1,
    ExitMonsterOptions = 2,
    /// <summary>Rebuild duel monster option row on every peer so MP <c>NetCombatCard</c> indices match for plays.</summary>
    OpenMonsterOptions = 3
}

/// <summary>
/// Network payload for <see cref="YgoMonsterMenuCommandGameAction"/>.
/// Unplayable <see cref="Cards.Command.MonsterCommandCard"/> options never enqueue <see cref="PlayCardAction"/> (see
/// <c>CardModel.TryManualPlay</c>), so host-only UI handlers desync; this action mirrors host and clients through
/// <see cref="MegaCrit.Sts2.Core.GameActions.Multiplayer.ActionQueueSynchronizer"/>.
/// </summary>
public struct NetYgoMonsterMenuCommandAction : INetAction, IPacketSerializable
{
    public YgoMonsterMenuCommandKind Kind;

    /// <summary>Source duel pet; unused for <see cref="YgoMonsterMenuCommandKind.ExitMonsterOptions"/>.</summary>
    public uint PetCombatId;

    public bool HasEnemyTarget;

    public uint EnemyTargetCombatId;

    public GameAction ToGameAction(Player player)
    {
        return new YgoMonsterMenuCommandGameAction(player, this);
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteEnum(Kind);
        writer.WriteUInt(PetCombatId, 16);
        writer.WriteBool(HasEnemyTarget);
        if (HasEnemyTarget)
            writer.WriteUInt(EnemyTargetCombatId, 16);
    }

    public void Deserialize(PacketReader reader)
    {
        Kind = reader.ReadEnum<YgoMonsterMenuCommandKind>();
        PetCombatId = reader.ReadUInt(16);
        HasEnemyTarget = reader.ReadBool();
        if (HasEnemyTarget)
            EnemyTargetCombatId = reader.ReadUInt(16);
        else
            EnemyTargetCombatId = 0;
    }
}
