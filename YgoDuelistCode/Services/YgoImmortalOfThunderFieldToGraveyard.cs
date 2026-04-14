using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="The_Immortal_of_Thunder"/>: when sent from the field to the Graveyard, take <c>Mgc2</c> unblockable damage.
/// </summary>
public static class YgoImmortalOfThunderFieldToGraveyard
{
    public static async Task RunAsync(Player player, The_Immortal_of_Thunder source)
    {
        if (player?.Creature?.CombatState == null)
            return;

        decimal dmg = source.DynamicVars["Mgc2"].BaseValue;
        if (dmg <= 0m)
            return;

        var ctx = new BlockingPlayerChoiceContext();
        await CreatureCmd.Damage(
            ctx,
            player.Creature,
            dmg,
            ValueProp.Unblockable | ValueProp.Unpowered,
            player.Creature,
            source);
    }
}
