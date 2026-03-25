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
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Dream_Clown : EffectMonsterCard
{
    public Dream_Clown()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override Task OnSwitchedFromAttackToDefenseFromCommandAsync(PlayerChoiceContext choiceContext, Player player) =>
        ResolveAttackToDefenseEffectAsync(choiceContext, player);

    private async Task ResolveAttackToDefenseEffectAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature?.CombatState == null)
            return;

        Creature? pet = null;
        foreach (Creature p in player.PlayerCombatState!.Pets)
        {
            if (p.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.GetSourceCardForPet(p) == this)
            {
                pet = p;
                break;
            }
        }

        if (pet == null || !pet.IsAlive)
            return;

        CombatState cs = player.Creature.CombatState;
        List<Creature> enemies = YgoDeterministicRng
            .StableOrder(cs.HittableEnemies.Where(c => c.IsAlive), c => c.CombatId)
            .ToList();
        if (enemies.Count == 0)
            return;

        Creature? target = enemies.Count == 1
            ? enemies[0]
            : YgoDeterministicRng.PickOne(cs, enemies, $"DREAM_CLOWN-{pet.CombatId}");

        if (target == null || !target.IsAlive)
            return;

        await PowerCmd.Apply<VulnerablePower>(target, 1m, pet, this);
        await DamageCmd.Attack(6m)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
