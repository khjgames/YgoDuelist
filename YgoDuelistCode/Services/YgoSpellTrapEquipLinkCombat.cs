using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoSpellTrapEquipLinkCombat
{
    /// <summary>Kills the field pet for <paramref name="monster"/> if it is still the registered source and alive.</summary>
    public static async Task DestroyLinkedMonsterIfOnFieldAsync(Player player, BaseMonsterCard monster)
    {
        if (player?.PlayerCombatState == null)
            return;

        Creature? pet = YgoMpCombatOrder.FirstPetWhere(player.PlayerCombatState, p =>
            p.IsAlive
            && p.Monster is DuelMonsterModel
            && DuelMonsterFieldRegistry.HasSourceCard(p, monster));

        if (pet != null)
            await CreatureCmd.Kill(pet, force: true);
    }
}
