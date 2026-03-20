using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Unplayable command: toggle whether this field monster is marked as tribute material (ordered list for the turn).
/// </summary>
public sealed class Command_Toggle_Tribute_Sacrifice : MonsterCommandCard
{
    public Command_Toggle_Tribute_Sacrifice()
    {
    }

    public Command_Toggle_Tribute_Sacrifice(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    public async Task OnClickedOption()
    {
        var player = Owner;
        if (player == null || SourceMonster == null)
            return;

        await TributeMaterialMarkTracker.ToggleMarkAsync(player, SourceMonster, player.Creature, this);
        YgoOptionHandBridge.RequestDeferredSyncFromOptionPile(player);
    }
}
