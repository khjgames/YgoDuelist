using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Command: this monster defends, giving you block equal to its current DEF.
/// </summary>
public sealed class Command_Defend : MonsterCommandCard
{
    // For reflection / scanners
    public Command_Defend()
    {
    }

    public Command_Defend(NormalMonsterCard source)
        : base(source, 1, CardType.Skill, TargetType.Self)
    {
    }

    protected override int CanonicalEnergyCost => 1;

    public new LocString Description
    {
        get
        {
            if (SourceMonster == null)
                return base.Description;

            var entry = SourceMonster.Id.Entry;
            return new LocString("cards", entry + ".description_skill_combat");
        }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster == null) return false;
            var pet = FindPetForMonster(SourceMonster);
            if (pet == null) return false;
            return !MonsterCommandRegistry.GetOrCreate(pet).HasUsedCommandThisTurn;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        GD.Print("[ZGO Command_Defend] OnPlay start");
        var player = Owner;
        if (player == null || SourceMonster == null)
        {
            GD.Print("[ZGO Command_Defend] Early return: player=", player != null, " SourceMonster=", SourceMonster != null);
            return;
        }
        GD.Print("[ZGO Command_Defend] Player found: ");

        var pet = FindPetForMonster(SourceMonster);
        GD.Print("[ZGO Command_Defend] Pet found: ", pet != null);
        if (pet != null)
        {
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(pet, true, player.Creature, SourceMonster);
        }

        SourceMonster.SetBattlePositionFromDuelCommand(attackPosition: false);
        await SourceMonster.CombatAction(choiceContext, cardPlay);
    }

    private static Creature? FindPetForMonster(BaseMonsterCard source)
    {
        var player = source.Owner;
        if (player?.PlayerCombatState == null)
            return null;

        return player.PlayerCombatState.Pets
            .FirstOrDefault(p => DuelMonsterFieldRegistry.GetSourceCardForPet(p) == source);
    }

}

