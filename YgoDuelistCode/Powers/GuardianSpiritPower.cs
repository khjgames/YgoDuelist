using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// When a duel monster on your field is destroyed: gain Block (Amount) and heal each other duel monster 1 HP.
/// </summary>
public sealed class GuardianSpiritPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "YGODUELIST-GUARDIAN_SPIRIT_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-GUARDIAN_SPIRIT_POWER.description");

    public static async Task OnPlayerDuelMonsterDestroyedAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        Creature destroyedPet)
    {
        if (player.Creature?.GetPower<GuardianSpiritPower>() is not { } power)
            return;

        decimal block = power.Amount;
        if (block > 0m)
            await CreatureCmd.GainBlock(player.Creature, block, default, null);

        if (player.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (pet == null || !pet.IsAlive || pet == destroyedPet)
                continue;
            if (pet.Monster is not DuelMonsterModel)
                continue;
            await CreatureCmd.Heal(pet, 1m);
        }
    }
}
