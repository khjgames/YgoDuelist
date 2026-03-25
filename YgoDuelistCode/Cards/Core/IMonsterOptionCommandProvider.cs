using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Command;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Optional extra monster-option commands (beyond the standard set and <see cref="IMonsterActivatedEffect"/>).
/// Field "Activate Effect" is added automatically when the monster implements <see cref="IMonsterActivatedEffect"/>.
/// </summary>
public interface IMonsterOptionCommandProvider
{
    IEnumerable<MonsterCommandCard> BuildExtraMonsterOptionCommands(CombatState combatState, Player player, Creature pet);
}

