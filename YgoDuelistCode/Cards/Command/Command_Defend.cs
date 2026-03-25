using System.Linq;
using System.Threading.Tasks;
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
        : base(source, source.DuelMonsterDefensePlayEnergy, CardType.Skill, TargetType.Self)
    {
    }

    protected override int CanonicalEnergyCost
    {
        get
        {
            if (SourceMonster == null)
                return 0;
            Creature? pet = FindPetForMonster(SourceMonster);
            if (pet != null && MonsterCommandRegistry.GetOrCreate(pet).ZeroEnergyMonsterCommandsThisTurn)
                return 0;
            return SourceMonster.DuelMonsterDefensePlayEnergy;
        }
    }

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
        var player = Owner;
        if (player == null || SourceMonster == null)
            return;

        var pet = FindPetForMonster(SourceMonster);
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

