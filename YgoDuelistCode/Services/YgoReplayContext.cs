using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

internal enum YgoReplayRoute
{
    SpellTrap,
    MonsterHandPlay,
    MonsterOutputOnly,
}

/// <summary>
/// Hand-summon <see cref="NormalMonsterCard.CombatAction"/> performed on the first paid play (attack damage or defend block).
/// </summary>
internal enum YgoReplayHandSummonCombatKind
{
    None,
    Attack,
    Defend,
}

internal sealed class YgoReplayContext
{
    public required CardModel SourceCard { get; init; }
    public required Player Player { get; init; }
    public required YgoReplayRoute Route { get; init; }
    public int ReplayCount { get; set; }
    public Creature? Target { get; set; }
    public BaseMonsterCard? EquipTarget { get; set; }
    public BaseMonsterCard? SummonedOutputMonster { get; set; }
    public YgoReplayHandSummonCombatKind HandSummonCombat { get; set; }

    /// <summary>
    /// <see cref="DuelMonsterSummon.TrySummonDuelMonster"/> <c>canAttackThisTurn</c> from the first hand play.
    /// When <c>false</c>, the duplicate gets stiff/fatigue like the original normal/tribute hand summon.
    /// </summary>
    public bool HandSummonCanAttackThisTurn { get; set; } = true;
}
