using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Blast_Juggler : EffectMonsterCard, IMonsterActivatedEffect
{
    public Blast_Juggler()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 8,
            baseDef: 9,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Blast_Juggler),
    };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Attack;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-BLAST_JUGGLER.activated_effect.description";

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        var player = source.Owner;
        if (player?.Creature?.CombatState == null || cardPlay.Target == null)
            return;

        CombatState cs = player.Creature.CombatState;
        Creature first = cardPlay.Target;

        var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source);
        if (pet == null)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);

        await CreatureCmd.Kill(pet, force: true);
        var grave = YgoPlayerPiles.Graveyard(player);
        if (grave != null)
            await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        decimal dmg = source.DynamicVars["Mgc"].BaseValue;
        await DamageCmd.Attack(dmg)
            .FromCard(source)
            .Targeting(first)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        List<Creature> rest = YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs)
            .Where(c => c != first)
            .ToList();

        if (rest.Count == 0)
            return;

        Creature? second = rest.Count == 1
            ? rest[0]
            : YgoDeterministicRng.PickOne(cs, rest, $"BlastJuggler2-{first.CombatId}");

        if (second == null || !second.IsAlive)
            return;

        await DamageCmd.Attack(dmg)
            .FromCard(source)
            .Targeting(second)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 13m;
    }
}
