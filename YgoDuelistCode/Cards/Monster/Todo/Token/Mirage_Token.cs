using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;

/// <summary>Token for <see cref="YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal.Physical_Double"/>: ATK/DEF/level are set when summoned.</summary>
public sealed class Mirage_Token : YgoTokenEffectMonster
{
    [SavedProperty]
    public int MirageAtk { get; set; }

    [SavedProperty]
    public int MirageDef { get; set; }

    /// <summary>When true, destroyed at end of turn (see <see cref="YgoDuelist.YgoDuelistCode.Services.YgoMirageTokenEndPhase"/>).</summary>
    [SavedProperty]
    public bool MirageDestroyAtEndOfTurn { get; set; }

    public Mirage_Token()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        int atk = MirageAtk - BaseAtk;
        int def = MirageDef - BaseDef;
        return (atk, def);
    }

    public void ApplyMirageStats(int atk, int def, int level)
    {
        MirageAtk = atk;
        MirageDef = def;
        MirageDestroyAtEndOfTurn = true;
        SetDuelMonsterLevel(level);
    }
}
