using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
public static class SpellbindingCircleTurnEffectPatch
{
    [HarmonyPostfix]
    public static async void Postfix(CombatState combatState, PlayerChoiceContext choiceContext, Player player)
    {
        if (combatState == null || combatState.CurrentSide != CombatSide.Player)
            return;

        var zonePile = SpellTrapZonePile.CustomType.GetPile(player);
        if (zonePile == null)
            return;

        var circles = zonePile.Cards.OfType<Spellbinding_Circle>().Where(c => !c.FaceDown).ToList();
        if (circles.Count == 0)
            return;

        var enemies = combatState.HittableEnemies.Where(c => c.IsAlive).ToList();
        if (enemies.Count == 0)
            return;

        foreach (Spellbinding_Circle sc in circles)
        {
            decimal strLoss = sc.DynamicVars["Mgc"].BaseValue;
            decimal spellbound = sc.DynamicVars["Mgc2"].BaseValue;
            foreach (Creature e in enemies)
            {
                await PowerCmd.Apply<YgoTemporaryStrengthLossPower>(e, strLoss, player.Creature, null);
                await PowerCmd.Apply<SpellboundPower>(e, spellbound, player.Creature, null);
            }
        }
    }
}
