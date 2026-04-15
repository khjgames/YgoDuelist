using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Arguments for <see cref="AbstractMonsterCard.OnPetDiedBeforeOptionPileHandlingAsync"/> and
/// <see cref="AbstractMonsterCard.OnPetDiedAfterOptionPileHandlingAsync"/>.</summary>
public readonly struct DuelMonsterPetDeathContext
{
    public DuelMonsterPetDeathContext(Player player, Creature pet, MonsterCommandState? commandState)
    {
        Player = player;
        Pet = pet;
        CommandState = commandState;
    }

    public Player Player { get; }
    public Creature Pet { get; }
    public MonsterCommandState? CommandState { get; }
}
