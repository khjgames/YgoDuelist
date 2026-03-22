using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Toggles this monster's "Die for You" behavior: when enabled, it gains DieForYouPower and tracks the player's block.
/// </summary>
public sealed class Toggle_Die_For_You : MonsterCommandCard
{
    // For reflection / scanners
    public Toggle_Die_For_You()
    {
    }

    public Toggle_Die_For_You(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    protected internal override bool ShowsEnergyCostIcon => false;

    public async Task OnClickedOption()
    {
        GD.Print("[ZGO] Toggle_Die_For_You.OnClickedOption() entered");
        var player = Owner;
        if (player == null || SourceMonster == null)
        {
            GD.Print("[ZGO_ERROR] Toggle_Die_For_You.OnClickedOption() early exit: player or SourceMonster null");
            return;
        }

        var pet = FindPetForMonster(SourceMonster, player);
        if (pet != null)
        {
            var state = MonsterCommandRegistry.GetOrCreate(pet);
            state.DieForYouEnabled = !state.DieForYouEnabled;

            if (state.DieForYouEnabled)
            {
                await PowerCmd.Apply<DieForYouPower>(pet, 1m, player.Creature, SourceMonster);
                var petNode = NCombatRoom.Instance?.GetCreatureNode(pet);
                petNode?.TrackBlockStatus(player.Creature);
            }
            else
            {
                await PowerCmd.Remove<DieForYouPower>(pet);
            }
        }

        GD.Print("[ZGO] Toggle_Die_For_You.OnClickedOption() done (option pile not cleared)");
    }

    private static Creature? FindPetForMonster(BaseMonsterCard source, Player player)
    {
        if (player.PlayerCombatState == null)
            return null;

        foreach (var pet in player.PlayerCombatState.Pets)
        {
            if (pet.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.GetSourceCardForPet(pet) == source)
                return pet;
        }
        return null;
    }
}
