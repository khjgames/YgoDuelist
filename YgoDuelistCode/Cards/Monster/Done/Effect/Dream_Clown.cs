using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
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

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Dream_Clown : EffectMonsterCard
{
    public Dream_Clown()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 12,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Dream_Clown),
    };

    public override Task OnSwitchedFromAttackToDefenseFromCommandAsync(PlayerChoiceContext choiceContext, Player player) =>
        ResolveAttackToDefenseEffectAsync(choiceContext, player);

    private async Task ResolveAttackToDefenseEffectAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature?.CombatState == null)
            return;

        Creature? pet = null;
        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (p.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.HasSourceCard(p, this))
            {
                pet = p;
                break;
            }
        }

        if (pet == null || !pet.IsAlive)
            return;

        CombatState cs = player.Creature.CombatState;
        List<Creature> enemies = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs);
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
