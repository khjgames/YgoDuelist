using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Cards;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// While <see cref="Bad_Reaction_to_Simochi"/> is face-up, enemy heals become damage (1× or 1.5× the heal amount).
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal), typeof(Creature), typeof(decimal), typeof(bool))]
public static class CreatureCmdHealBadReactionToSimochiPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Creature creature, decimal amount, bool playAnim, ref Task __result)
    {
        _ = playAnim;
        if (amount <= 0m || creature == null || creature.IsPlayer)
            return true;

        var cs = creature.CombatState;
        if (cs == null || !YgoBadReactionToSimochi.IsCombatEnemy(cs, creature))
            return true;

        if (!YgoBadReactionToSimochi.TryResolveBest(cs, out CardModel? sourceCard, out decimal mult))
            return true;

        Creature? dealer = sourceCard?.Owner?.Creature;
        if (dealer == null)
            return true;

        decimal damage = amount * mult;
        __result = CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            creature,
            damage,
            ValueProp.Unpowered,
            dealer,
            sourceCard);
        return false;
    }
}
