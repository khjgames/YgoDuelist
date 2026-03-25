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

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Blast_Juggler : EffectMonsterCard, IMonsterActivatedEffect
{
    public Blast_Juggler()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 8,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Machine)
    {
    }

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
        var grave = GraveyardPile.CustomType.GetPile(player);
        if (grave != null)
            await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        await DamageCmd.Attack(10m)
            .FromCard(source)
            .Targeting(first)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        List<Creature> rest = YgoDeterministicRng
            .StableOrder(
                cs.HittableEnemies.Where(c => c.IsAlive && c != first),
                c => c.CombatId)
            .ToList();

        if (rest.Count == 0)
            return;

        Creature? second = rest.Count == 1
            ? rest[0]
            : YgoDeterministicRng.PickOne(cs, rest, $"BlastJuggler2-{first.CombatId}");

        if (second == null || !second.IsAlive)
            return;

        await DamageCmd.Attack(10m)
            .FromCard(source)
            .Targeting(second)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
