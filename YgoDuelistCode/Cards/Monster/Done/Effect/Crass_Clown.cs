using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

public sealed class Crass_Clown : EffectMonsterCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", 6m) });

    public Crass_Clown()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 13,
            baseDef: 14,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Fiend)
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
        typeof(Crass_Clown),
    };

    public override Task OnSwitchedFromDefenseToAttackFromCommandAsync(PlayerChoiceContext choiceContext, Player player)
    {
        return ResolveDefenseToAttackEffectAsync(choiceContext, player);
    }

    private async Task ResolveDefenseToAttackEffectAsync(PlayerChoiceContext choiceContext, Player player)
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
            : YgoDeterministicRng.PickOne(cs, enemies, $"CRASS_CLOWN-{pet.CombatId}");

        if (target == null || !target.IsAlive)
            return;

        await PowerCmd.Apply<WeakPower>(target, DynamicVars["Mgc"].BaseValue, pet, this);
        await DamageCmd.Attack(DynamicVars["Mgc2"].BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
        DynamicVars["Mgc2"].BaseValue = 9m;
    }
}
