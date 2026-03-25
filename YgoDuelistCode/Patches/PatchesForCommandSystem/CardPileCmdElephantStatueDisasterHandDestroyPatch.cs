using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="Elephant_Statue_of_Disaster"/>: when sent from hand to the Graveyard, deal 20 to an enemy
/// (deterministic random target when several hittable enemies exist).
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
public static class CardPileCmdElephantStatueDisasterHandDestroyPatch
{
    private static readonly Dictionary<CombatState, int> s_seq = new();
    private static readonly object s_seqLock = new();

    [HarmonyPrefix]
    public static void Prefix(IEnumerable<CardModel> cards, CardPile newPile, ref List<(CardModel Card, PileType? From)>? __state)
    {
        __state = new List<(CardModel, PileType?)>();
        foreach (CardModel c in cards)
            __state.Add((c, c.Pile?.Type));
    }

    [HarmonyPostfix]
    public static void Postfix(Task __result, CardPile newPile, List<(CardModel Card, PileType? From)>? __state)
    {
        if (__state == null || newPile.Type != GraveyardPile.CustomType)
            return;

        _ = AfterGraveyardAddAsync(__result, __state);
    }

    private static async Task AfterGraveyardAddAsync(Task moveCompleted, List<(CardModel Card, PileType? From)> state)
    {
        await moveCompleted;

        foreach ((CardModel card, PileType? from) in state)
        {
            if (from != PileType.Hand || card is not Elephant_Statue_of_Disaster elephant)
                continue;

            Player? player = elephant.Owner;
            Creature? pc = player?.Creature;
            CombatState? cs = pc?.CombatState;
            if (cs == null)
                continue;

            List<Creature> enemies = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0)
                continue;

            Creature? target = enemies.Count == 1
                ? enemies[0]
                : YgoDeterministicRng.PickOne(cs, enemies, $"ELEPHANT_STATUE_DISASTER_GY-{NextSeq(cs)}");

            if (target == null || !target.IsAlive)
                continue;

            var ctx = new BlockingPlayerChoiceContext();
            await DamageCmd.Attack(20m)
                .FromCard(elephant)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(ctx);
        }
    }

    private static int NextSeq(CombatState cs)
    {
        lock (s_seqLock)
        {
            s_seq.TryGetValue(cs, out int n);
            int next = n + 1;
            s_seq[cs] = next;
            return next;
        }
    }
}
