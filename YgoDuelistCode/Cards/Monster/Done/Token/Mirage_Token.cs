using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;

/// <summary>Token for <see cref="YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal.Physical_Double"/>: ATK/DEF/level are set when summoned.</summary>
public sealed class Mirage_Token : YgoTokenEffectMonster, IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect
{
    [SavedProperty]
    public int YgoDuelist_MirageAtk { get; set; }

    [SavedProperty]
    public int YgoDuelist_MirageDef { get; set; }

    /// <summary>When true, destroyed at end of turn via <see cref="IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect"/>.</summary>
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
            duelMonsterRace: DuelMonsterRace.Warrior,
            duelMonsterDefensePlayEnergyOverride: 1,
            duelMonsterAttackPlayEnergyOverride: 1)
    {
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        int atk = YgoDuelist_MirageAtk - BaseAtk;
        int def = YgoDuelist_MirageDef - BaseDef;
        return (atk, def);
    }

    public void ApplyMirageStats(int atk, int def, int level)
    {
        YgoDuelist_MirageAtk = atk;
        YgoDuelist_MirageDef = def;
        MirageDestroyAtEndOfTurn = true;
        SetDuelMonsterLevel(level);
    }

    public bool IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(Creature pet) =>
        MirageDestroyAtEndOfTurn && pet.IsAlive;

    public async Task TryResolveOwnerBeforeTurnEndFlushFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner, Creature pet)
    {
        if (!IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(pet))
            return;
        await CreatureCmd.Kill(pet, force: true);
    }
}
