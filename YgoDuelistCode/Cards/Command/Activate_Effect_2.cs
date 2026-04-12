using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Second optional field effect on monsters implementing <see cref="IMonsterSecondActivatedEffect"/>.
/// </summary>
public sealed class Activate_Effect_2 : MonsterCommandCard
{
    private IMonsterSecondActivatedEffect? Effect => SourceMonster as IMonsterSecondActivatedEffect;

    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    public Activate_Effect_2()
    {
    }

    protected override int CanonicalEnergyCost
    {
        get
        {
            if (SourceMonster is not IMonsterSecondActivatedEffect impl)
                return 0;
            var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            if (pet != null && MonsterCommandRegistry.GetOrCreate(pet).ZeroEnergyMonsterCommandsThisTurn)
                return 0;
            return impl.SecondActivatedEffectEnergyCost;
        }
    }

    public override CardType Type => Effect?.SecondActivatedEffectCardType ?? CardType.Skill;

    public override TargetType TargetType => Effect?.SecondActivatedEffectTarget ?? TargetType.Self;

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster is not IMonsterSecondActivatedEffect impl)
                return false;
            if (SourceMonster.FaceDown)
                return false;
            if (!impl.IsSecondActivatedEffectAvailable)
                return false;
            var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            if (pet == null)
                return false;
            bool regularDeckBypass = IsRegularDeckMonsterCommandWithLivePet(pet);
            if (impl.SecondActivatedEffectConsumesOncePerTurnSlot && !regularDeckBypass)
            {
                MonsterCommandState reg = MonsterCommandRegistry.GetOrCreate(pet);
                if (reg.HasUsedSecondActivatedEffectThisTurn)
                    return false;
            }

            return true;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || SourceMonster is not IMonsterSecondActivatedEffect impl || SourceMonster.FaceDown)
            return;

        var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
        if (pet != null)
            await YgoNarrowPassField.ApplyMonsterCommandLifePaymentIfActiveAsync(choiceContext, Owner, pet);

        await impl.OnSecondActivatedEffect(choiceContext, cardPlay, SourceMonster);
    }
}
