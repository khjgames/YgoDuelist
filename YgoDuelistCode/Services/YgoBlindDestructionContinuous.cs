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

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(player);
        if (zone == null)
            return;

        List<Blind_Destruction> active = zone.Cards
            .OfType<Blind_Destruction>()
            .Where(c => !c.FaceDown)
            .ToList();

        if (active.Count == 0)
            return;

        if (!YgoAnnualTracker.TryConsumeAnnual(player, "BLIND_DESTRUCTION"))
            return;

        CombatState? cs = player.Creature.CombatState;
        if (cs == null)
            return;

        var rolls = new List<(Blind_Destruction Src, int Roll)>(active.Count);
        var resultCards = new List<CardModel>(active.Count);

        foreach (Blind_Destruction src in active)
        {
            ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(player, src);
            int roll = YgoDeterministicRng.RollDie(cs, 6, "BLIND_DESTRUCTION-D6", mix);
            rolls.Add((src, roll));
            resultCards.Add(YgoDeterministicRngResultDisplay.CreateD6RollResultCard(cs, player, roll));
        }

        LocString prompt = active.Count == 1
            ? MakeSingleRollPrompt(rolls[0].Roll)
            : MakeMultiRollPrompt(rolls);

        var prefs = new CardSelectorPrefs(prompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await CardSelectCmd.FromSimpleGrid(choiceContext, resultCards, player, prefs);

        foreach ((Blind_Destruction src, int roll) in rolls)
            await ApplyOneDie(choiceContext, player, cs, src, roll);
    }

    private static LocString MakeSingleRollPrompt(int roll)
    {
        var p = new LocString("cards", "YGODUELIST-BLIND_DESTRUCTION.die_result.selection");
        p.Add("Roll", (decimal)roll);
        return p;
    }

    private static LocString MakeMultiRollPrompt(IReadOnlyList<(Blind_Destruction Src, int Roll)> rolls)
    {
        var p = new LocString("cards", "YGODUELIST-BLIND_DESTRUCTION.die_result.selection_multi");
        p.Add("Rolls", string.Join(", ", rolls.Select(r => r.Roll.ToString())));
        return p;
    }

    private static async Task ApplyOneDie(
        PlayerChoiceContext choiceContext,
        Player player,
        CombatState cs,
        Blind_Destruction src,
        int roll)
    {
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
