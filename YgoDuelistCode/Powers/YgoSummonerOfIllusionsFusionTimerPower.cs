using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Marks a Fusion monster Special Summoned by Summoner of Illusions; destroyed at end of the combat turn in which it was summoned.</summary>
public sealed class YgoSummonerOfIllusionsFusionTimerPower : YgoDuelistPower
{
    public override bool IsInstanced => true;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-SUMMONER_OF_ILLUSIONS_FUSION_TIMER_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SUMMONER_OF_ILLUSIONS_FUSION_TIMER_POWER.description");

    private CombatSide _destroyOnSide;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        CombatState? cs = Owner?.CombatState;
        _destroyOnSide = cs?.CurrentSide ?? CombatSide.Player;
        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != _destroyOnSide || Owner.Side != CombatSide.Player)
            return;

        if (CombatManager.Instance.IsOverOrEnding)
        {
            await PowerCmd.Remove(this);
            return;
        }

        if (!Owner.IsAlive)
        {
            await PowerCmd.Remove(this);
            return;
        }

        await CreatureCmd.Kill(Owner, force: true);
    }
}
