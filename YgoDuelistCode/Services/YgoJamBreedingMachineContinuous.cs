using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>While <see cref="Jam_Breeding_Machine"/> is face-up in the Spell/Trap zone: each of your turn starts, Special Summon 1 Slime Token.</summary>
public static class YgoJamBreedingMachineContinuous
{
    public static async Task TryResolvePlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState == null)
            return;

        var zone = SpellTrapZoneRelic.GetSpellTrapZonePile(player);
        if (zone == null || !zone.Cards.OfType<Jam_Breeding_Machine>().Any(c => !c.FaceDown))
            return;

        if (!YgoAnnualTracker.TryConsumeAnnual(player, "JAM_BREEDING_MACHINE"))
            return;

        await YgoTokenSummon.TrySpecialSummonTokenAsync<Slime_Token>(player, choiceContext, defensePosition: false);
    }
}
