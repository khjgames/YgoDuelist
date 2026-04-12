using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Insect_Queen : EffectMonsterCard
{
    /// <summary>Set when this card destroys an enemy by battle; consumed at End Phase for Insect Monster Token.</summary>
    [SavedProperty]
    public bool PendingInsectMonsterTokenEndPhase { get; set; }

    public Insect_Queen()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 7,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 22,
            baseDef: 24,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    protected override Task OnAfterMonsterAttackHitAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        AttackCommand attackCommand)
    {
        foreach (var r in attackCommand.Results)
        {
            if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                continue;
            PendingInsectMonsterTokenEndPhase = true;
            break;
        }

        return Task.CompletedTask;
    }
}
