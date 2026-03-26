using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>While <see cref="Blind_Destruction"/> is face-up in the Spell/Trap zone (no player power).</summary>
public static class YgoBlindDestructionContinuous
{
    public static async Task TryResolvePlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == null)
            return;

        Blind_Destruction? src = SpellTrapZonePile.CustomType.GetPile(player)?.Cards.OfType<Blind_Destruction>().FirstOrDefault();
        if (src == null)
            return;

        if (!YgoAnnualTracker.TryConsumeAnnual(player, "BLIND_DESTRUCTION"))
            return;

        var cs = player.Creature.CombatState;
        if (cs == null)
            return;

        ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(player, src);
        int roll = YgoDeterministicRng.RollDie(cs, 6, "BLIND_DESTRUCTION-D6", mix);

        var faceCards = new List<CardModel>();
        for (int f = 1; f <= 6; f++)
            faceCards.Add(new YgoDieFaceProxyCard(f));

        var prompt = new LocString("cards", "YGODUELIST-BLIND_DESTRUCTION.die_faces.selection");
        prompt.Add("Roll", (decimal)roll);
        var prefs = new CardSelectorPrefs(prompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await CardSelectCmd.FromSimpleGrid(choiceContext, faceCards, player, prefs);

        decimal sixCase = src.DynamicVars["Mgc"].BaseValue;
        decimal dmg = roll == 6 ? sixCase : roll;

        foreach (Creature e in cs.HittableEnemies.Where(c => c.IsAlive))
            await CreatureCmd.Damage(choiceContext, e, dmg, ValueProp.Unpowered, player.Creature, src);

        if (roll == 6)
            return;

        if (player.PlayerCombatState == null)
            return;

        foreach (Creature pet in player.PlayerCombatState.Pets.ToList())
        {
            if (pet is { IsAlive: true })
                await CreatureCmd.Damage(choiceContext, pet, dmg, ValueProp.Unpowered, player.Creature, src);
        }
    }
}
