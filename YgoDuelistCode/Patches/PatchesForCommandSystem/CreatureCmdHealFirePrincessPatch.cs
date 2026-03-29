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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>
/// <see cref="Fire_Princess"/>: when the player gains HP from <see cref="CreatureCmd.Heal"/>, each face-up copy on the field
/// deals <c>Mgc</c> damage to a random hittable enemy (deterministic per instance when several enemies exist).
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal), typeof(Creature), typeof(decimal), typeof(bool))]
public static class CreatureCmdHealFirePrincessPatch
{
    private const decimal SkipSentinel = -99999m;

    private static readonly Dictionary<CombatState, int> s_seq = new();
    private static readonly object s_seqLock = new();

    [HarmonyPrefix]
    public static void Prefix(Creature creature, decimal amount, ref decimal __state)
    {
        __state = SkipSentinel;
        if (creature == null || !creature.IsPlayer || amount <= 0m)
            return;

        __state = creature.CurrentHp;
    }

    [HarmonyPostfix]
    public static void Postfix(Task __result, Creature creature, decimal amount, bool playAnim, decimal __state)
    {
        _ = playAnim;
        if (__state == SkipSentinel)
            return;

        _ = AfterHealAsync(__result, creature, __state);
    }

    private static async Task AfterHealAsync(Task healDone, Creature creature, decimal hpBeforeHeal)
    {
        await healDone;

        if (creature == null || !creature.IsPlayer)
            return;

        decimal gained = creature.CurrentHp - hpBeforeHeal;
        if (gained <= 0m)
            return;

        Player? player = creature.Player;
        CombatState? cs = creature.CombatState;
        if (player == null || cs == null)
            return;

        List<Creature> enemies = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
            return;

        foreach (BaseMonsterCard card in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (card is not Fire_Princess fp || fp.FaceDown)
                continue;

            if (player.PlayerCombatState == null
                || !player.PlayerCombatState.Pets.Any(p =>
                    p.IsAlive && DuelMonsterFieldRegistry.GetSourceCardForPet(p) == fp))
                continue;

            decimal dmg = fp.DynamicVars["Mgc"].BaseValue;
            if (dmg <= 0m)
                continue;

            Creature? target = enemies.Count == 1
                ? enemies[0]
                : YgoDeterministicRng.PickOne(cs, enemies, $"FIRE_PRINCESS-{fp.Id.Entry}-{NextSeq(cs)}");

            if (target == null || !target.IsAlive)
                continue;

            var ctx = new BlockingPlayerChoiceContext();
            await DamageCmd.Attack(dmg)
                .FromCard(fp)
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
