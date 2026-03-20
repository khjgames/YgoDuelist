using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Unplayable command option: switching battle position runs via the same "cannot play" path as
/// <see cref="Toggle_Die_For_You"/>. Title shows the next action (attack vs defense) based on current position.
/// Applies <see cref="StiffPower"/> only (no <see cref="FatiguePower"/>, no command-slot consumption).
/// </summary>
public sealed class Command_Change_Battle_Position : MonsterCommandCard
{
    public Command_Change_Battle_Position()
    {
    }

    public Command_Change_Battle_Position(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    public async Task OnClickedOption()
    {
        var player = Owner;
        if (player == null || SourceMonster is not AbstractMonsterCard monster)
            return;

        var pet = FindPetForMonster(SourceMonster, player);
        if (pet != null && pet.HasPower<StiffPower>())
            return;

        monster.ApplyBattlePositionChangeFromCommandMenu();

        if (pet != null)
            await MonsterCommandRegistry.ApplyStiffFromBattlePositionChangeOnly(pet, player.Creature, this);

        YgoOptionHandBridge.RequestDeferredSyncFromOptionPile(player);
    }

    private static Creature? FindPetForMonster(NormalMonsterCard source, Player player)
    {
        if (player.PlayerCombatState == null)
            return null;

        return player.PlayerCombatState.Pets
            .FirstOrDefault(p => p.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.GetSourceCardForPet(p) == source);
    }
}
