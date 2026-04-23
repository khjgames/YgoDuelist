using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// After each YGO Spell resolves, if <see cref="YgoCurseOfDarknessField"/> is active, deal accumulated post-spell contributor damage to a deterministically chosen random enemy.
/// </summary>
public static class YgoCurseOfDarknessSpellHook
{
    private static readonly Dictionary<CombatState, int> s_spellResolveSeq = new();
    private static readonly object s_seqLock = new();

    public static void ClearAll()
    {
        lock (s_seqLock)
            s_spellResolveSeq.Clear();
    }

    public static async Task AfterSpellResolved(PlayerChoiceContext choiceContext, BaseSpellCard spell)
    {
        if (spell is not IYgoCard y || y.YgoCardType != YgoCardType.Spell)
            return;

        Player? player = spell.Owner;
        if (player?.Creature?.CombatState == null)
            return;

        if (!YgoCurseOfDarknessField.IsActive(player))
            return;

        decimal damage = YgoCurseOfDarknessField.GetTotalMgcDamage(player);
        if (damage <= 0m)
            return;

        CombatState cs = player.Creature.CombatState;
        List<Creature> enemies = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs);
        if (enemies.Count == 0)
            return;

        int seq = NextSeq(cs);
        string salt = "CURSE_OF_DARKNESS_SPELL_" + seq;
        Creature? victim = YgoDeterministicRng.PickOne(cs, enemies, salt);
        if (victim == null)
            return;

        CardModel damageSource = YgoCurseOfDarknessField.GetFirstActiveContributor(player) ?? spell;

        await CreatureCmd.Damage(choiceContext, victim, damage, ValueProp.Unpowered, player.Creature, damageSource);
    }

    private static int NextSeq(CombatState cs)
    {
        lock (s_seqLock)
        {
            s_spellResolveSeq.TryGetValue(cs, out int n);
            int next = n + 1;
            s_spellResolveSeq[cs] = next;
            return next;
        }
    }
}
