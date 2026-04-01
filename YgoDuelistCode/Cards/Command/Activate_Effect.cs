using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Single monster-options command; behavior and display come from <see cref="IMonsterActivatedEffect"/> on <see cref="MonsterCommandCard.SourceMonster"/>.
/// </summary>
public sealed class Activate_Effect : MonsterCommandCard
{
    private IMonsterActivatedEffect? Effect => SourceMonster as IMonsterActivatedEffect;

    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    public Activate_Effect()
    {
    }

    public override CardType Type => Effect?.ActivatedEffectCardType ?? CardType.Skill;

    public override TargetType TargetType => Effect?.ActivatedEffectTarget ?? TargetType.Self;

    protected override int CanonicalEnergyCost
    {
        get
        {
            int baseCost = Effect?.ActivatedEffectEnergyCost ?? 0;
            return baseCost + YgoNarrowPassField.GetMonsterCommandEnergyAdd(SourceMonster?.Owner);
        }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster is not IMonsterActivatedEffect impl)
                return false;
            if (SourceMonster.FaceDown)
                return false;
            if (!IsActivatedEffectAvailableInContext(SourceMonster, impl, Owner))
                return false;
            var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            if (pet == null)
                return false;
            bool regularDeckBypass = IsRegularDeckMonsterCommandWithLivePet(pet);
            if (impl.ActivatedEffectConsumesOncePerTurnSlot && !regularDeckBypass)
            {
                MonsterCommandState reg = MonsterCommandRegistry.GetOrCreate(pet);
                if (reg.HasUsedActivatedEffectThisTurn)
                    return false;
            }
            return true;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || SourceMonster is not IMonsterActivatedEffect impl || SourceMonster.FaceDown)
            return;

        var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
        if (pet != null)
            await YgoNarrowPassField.ApplyMonsterCommandLifePaymentIfActiveAsync(choiceContext, Owner, pet);

        await impl.OnActivatedEffect(choiceContext, cardPlay, SourceMonster);
    }

    /// <summary>
    /// Earth-tribute activated effects must see the same player as this command card when the source monster's Owner is not yet wired.
    /// </summary>
    private static bool IsActivatedEffectAvailableInContext(NormalMonsterCard source, IMonsterActivatedEffect impl, Player? commandOwner)
    {
        if (source is Arcane_Archer_of_the_Forest aa)
            return aa.IsEarthTributeAvailable(commandOwner);
        if (source is Anti_Aircraft_Flower af)
            return af.IsEarthTributeAvailable(commandOwner);
        if (source is The_Little_Swordsman_of_Aile little)
            return little.IsAnotherMonsterControlled(commandOwner);
        if (source is Winged_Minion winged)
            return winged.IsAnotherFiendControlled(commandOwner);
        return impl.IsActivatedEffectAvailable;
    }
}
