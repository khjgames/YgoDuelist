using System;
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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Dice_Jar : EffectMonsterCard
{
    private static readonly LocString RollPreviewPrompt =
        new("cards", "YGODUELIST-DICE_JAR.roll_result.selection");

    private static readonly LocString FlipEffectHoverTitle = new("card_keywords", "20041.title");

    private static readonly LocString FlipEffectHoverDescription =
        new("cards", "YGODUELIST-DICE_JAR.flip_effect.description");

    public Dice_Jar()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 2,
            baseDef: 3,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Chance;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Dice_Jar),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return new HoverTip(FlipEffectHoverTitle, FlipEffectHoverDescription);
            yield return YgoDeterministicRngResultDisplay.Rolled6SampleHoverTip();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = Owner;
        if (owner != null && CanSummonDuelMonster)
        {
            int tribute = TributeReleaseCount;
            if (tribute > 0)
            {
                if (!TributeSummonPlayPayload.TryTakePending(this, out var mats) || mats == null || mats.Count < tribute)
                {
                    if (owner.Creature != null)
                        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.AttackAnimDelay);
                    if (!ShouldSkipCombatActionAfterSummon(cardPlay))
                        await RunPostSummonCombatAsync(choiceContext, cardPlay);
                    return;
                }

                foreach (Creature pet in mats)
                    await CreatureCmd.Kill(pet, force: true);
            }

            await DuelMonsterSummon.TrySummonDuelMonster(owner, this, choiceContext);
        }

        if (owner?.Creature != null)
            await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.AttackAnimDelay);

        await RunPostSummonCombatAsync(choiceContext, cardPlay);
        await OnAfterMonsterPlayResolved(choiceContext, cardPlay);
    }

    private async Task RunPostSummonCombatAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Type == CardType.Attack)
            await RunDiceJarAttackAsync(choiceContext, cardPlay);
        else
            await RunDefenseCombatActionAsync(choiceContext, cardPlay);
    }

    private async Task RunDefenseCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int def = BaseDef;
        if (Owner != null)
        {
            var field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner)?.ToList() ?? new List<BaseMonsterCard>();
            if (!field.Contains(this))
                field.Add(this);
            def = CalcDuelMonsterStats(field).Def;
        }

        if (Owner?.Creature == null)
            return;

        WillSet = false;
        await CreatureCmd.GainBlock(Owner.Creature, (decimal)def, ValueProp.Move, cardPlay);
        await OnAfterGainBlockFromCombatActionAsync(choiceContext, cardPlay, def);
    }

    /// <summary>Attack combat from hand (after summon) or from field <c>Command_Attack</c>.</summary>
    public async Task RunDiceJarAttackAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player?.Creature?.CombatState is not CombatState cs)
            return;

        Creature playerCreature = player.Creature;
        Creature? pet = player.PlayerCombatState?.Pets
            .FirstOrDefault(p => DuelMonsterFieldRegistry.GetSourceCardForPet(p) == this);
        ulong mix = YgoDeterministicRng.MixDuelMonsterAttack(playerCreature, pet, cardPlay);

        int round = 0;
        int playerFace;
        int enemyFace;
        decimal playerScore;
        decimal enemyScore;
        do
        {
            round++;
            playerFace = YgoDeterministicRng.RollDie(cs, 6, $"DICE_JAR-P-{round}", mix);
            enemyFace = YgoDeterministicRng.RollDie(cs, 6, $"DICE_JAR-E-{round}", mix);
            playerScore = ScaledD6(playerFace);
            enemyScore = ScaledD6(enemyFace);
        } while (playerScore == enemyScore);

        var preview = new List<CardModel>
        {
            YgoDeterministicRngResultDisplay.CreateD6RollResultCard(cs, player, playerFace),
            YgoDeterministicRngResultDisplay.CreateD6RollResultCard(cs, player, enemyFace)
        };
        var prefs = new CardSelectorPrefs(RollPreviewPrompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await CardSelectCmd.FromSimpleGrid(choiceContext, preview, player, prefs);

        WillSet = false;

        if (playerScore > enemyScore)
        {
            decimal dmgEach = DynamicVars["Mgc"].BaseValue * playerScore;
            foreach (Creature enemy in YgoDeterministicRng.StableOrder(
                         cs.HittableEnemies.Where(c => c.IsAlive),
                         c => c.CombatId))
            {
                await DamageCmd.Attack(dmgEach)
                    .FromCard(this)
                    .Targeting(enemy)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            }
        }
        else if (player.PlayerCombatState != null)
        {
            foreach (Creature summon in YgoDeterministicRng.StableOrder(
                         player.PlayerCombatState.Pets.Where(
                             p => p.Monster is DuelMonsterModel && p.IsAlive),
                         p => p.CombatId))
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    summon,
                    enemyScore,
                    ValueProp.Unpowered,
                    playerCreature,
                    this);
            }
        }
    }

    private static decimal ScaledD6(int face) => face == 6 ? 9m : face;

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 5m;
    }
}
