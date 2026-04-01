using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace YgoDuelist.YgoDuelistCode.Models;

/// <summary>
/// Summoned ally creature from a monster card. HP is derived from level (star count).
/// Uses a static-image visuals scene; portrait art is swapped at runtime.
/// </summary>
public sealed class DuelMonsterModel : MonsterModel
{
    private int _level = 1;
    private string? _locTable;
    private string? _locKey;
    private string? _portraitPath;

    /// <summary>Level (star count) 1-9+. Set on mutable instance before CreateCreature.</summary>
    public int Level
    {
        get => _level;
        set
        {
            AssertMutable();
            _level = value;
        }
    }

    public void SetTitleLoc(string locTable, string locKey)
    {
        AssertMutable();
        _locTable = locTable;
        _locKey = locKey;
    }

    public void SetPortraitPath(string? portraitPath)
    {
        AssertMutable();
        _portraitPath = portraitPath;
    }

    public string? PortraitPath => _portraitPath;

    public override LocString Title
    {
        get
        {
            if (_locTable == null || _locKey == null)
                throw new InvalidOperationException("DuelMonsterModel Title used before SetTitleLoc was called.");
            return new LocString(_locTable, _locKey);
        }
    }

    // Make the HP bar much narrower than normal monsters so multiple summons fit cleanly.
    public override float HpBarSizeReduction => 100f;

    // Use a dedicated static-image duel monster scene (created per the static enemy guide).
    protected override string VisualsPath => "res://YgoDuelist/monsters/duel_monster/duel_monster.tscn";

    public override int MinInitialHp => HpFromLevel(_level);
    public override int MaxInitialHp => HpFromLevel(_level);

    /// <summary>HP by level: 1→3, 2→4, 3→6, 4→7, 5→10, 6→12, 7→15, 8→17, 9+→20.</summary>
    public static int HpFromLevel(int level)
    {
        return level switch
        {
            1 => 3,
            2 => 4,
            3 => 6,
            4 => 7,
            5 => 10,
            6 => 12,
            7 => 15,
            8 => 17,
            9 => 20,
            10 => 22,
            11 => 24,
            _ => 26
        };
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState nothing = new MoveState("NOTHING", NothingMove, new HiddenIntent());
        nothing.FollowUpState = nothing;
        return new MonsterMoveStateMachine(new List<MonsterState> { nothing }, nothing);
    }

    private static Task NothingMove(IReadOnlyList<Creature> _) => Task.CompletedTask;
}
