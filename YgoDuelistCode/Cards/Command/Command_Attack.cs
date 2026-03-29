using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Command: this monster attacks, dealing damage equal to its current ATK.
/// </summary>
public sealed class Command_Attack : MonsterCommandCard
{
    // For reflection / scanners
    public Command_Attack()
    {
    }

    public Command_Attack(NormalMonsterCard source)
        : base(source, source.DuelMonsterAttackPlayEnergy, CardType.Attack, TargetType.AnyEnemy)
    {
    }

    protected override int CanonicalEnergyCost
    {
        get
        {
            if (SourceMonster == null)
                return 0;
            Creature? pet = FindPetForMonster(SourceMonster);
            if (pet != null && MonsterCommandRegistry.GetOrCreate(pet).ZeroEnergyMonsterCommandsThisTurn)
                return 0;
            int baseCost = SourceMonster.DuelMonsterAttackPlayEnergy;
            return baseCost + YgoNarrowPassField.GetMonsterCommandEnergyAdd(SourceMonster.Owner);
        }
    }

    /// <summary>Always Attack so the card frame is correct when created from canonical (parameterless) instance.</summary>
    public override CardType Type => CardType.Attack;

    /// <summary>Always targets an enemy so option-row play gets targeting arrows and does not highlight the player. Parameterless ctor chains to base(Self); this override fixes that.</summary>
    public override TargetType TargetType => TargetType.AnyEnemy;

    public new LocString Description
    {
        get
        {
            if (SourceMonster == null)
                return base.Description;

            var entry = SourceMonster.Id.Entry;
            return new LocString("cards", entry + ".description_combat");
        }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster == null) return false;
            var pet = FindPetForMonster(SourceMonster);
            if (pet == null) return false;
            if (pet.HasPower<YgoStumblingDefendOnlyPower>())
                return false;
            if (SourceMonster is Dark_Zebra && Owner != null)
            {
                var field = DuelMonsterFieldRegistry.GetFieldMonsters(Owner)?.ToList() ?? [];
                if (field.Count == 1 && field[0] == SourceMonster)
                    return false;
            }

            return MonsterCommandRegistry.CanUseMonsterAttackCommand(pet, SourceMonster);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null || cardPlay.Target == null || SourceMonster == null)
            return;

        var pet = FindPetForMonster(SourceMonster);
        if (pet != null)
        {
            await YgoNarrowPassField.ApplyMonsterCommandLifePaymentIfActiveAsync(choiceContext, player, pet);
            await MonsterCommandRegistry.CommitMonsterCommandAfterPlay(pet, isAttackCommand: true, player.Creature, SourceMonster);
        }

        bool wasFaceDownDefense =
            SourceMonster is Stealth_Bird
            && !SourceMonster.IsAttackBattlePosition
            && SourceMonster.FaceDown;

        SourceMonster.SetBattlePositionFromDuelCommand(attackPosition: true);

        if (SourceMonster is Stealth_Bird bird && wasFaceDownDefense && cardPlay.Target != null)
        {
            await Stealth_Bird.DealFlipSummonDamageIfEligibleAsync(
                choiceContext,
                bird,
                wasFaceDownDefense,
                cardPlay.Target,
                player.Creature);
        }

        if (SourceMonster is Jirai_Gumo gumo)
            await Jirai_Gumo.RunAttackDeclarationCoinIfEligibleAsync(choiceContext, player, player.Creature, gumo, cardPlay);

        if (SourceMonster is Dice_Jar diceJar)
            await diceJar.RunDiceJarAttackAsync(choiceContext, cardPlay);
        else
            await SourceMonster.CombatAction(choiceContext, cardPlay);
    }

    private static Creature? FindPetForMonster(NormalMonsterCard source)
    {
        var player = source.Owner;
        if (player?.PlayerCombatState == null)
            return null;

        return player.PlayerCombatState.Pets
            .FirstOrDefault(p => DuelMonsterFieldRegistry.GetSourceCardForPet(p) == source);
    }
}
