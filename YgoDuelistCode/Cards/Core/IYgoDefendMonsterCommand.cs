using MegaCrit.Sts2.Core.Entities.Cards;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Marker for the field monster's defend command card. Post-play hooks can dispatch deferred block without referencing <see cref="YgoDuelist.YgoDuelistCode.Cards.Command.Command_Defend"/> concretely.
/// </summary>
public interface IYgoDefendMonsterCommand
{
    NormalMonsterCard? SourceMonster { get; }
}
