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

    public Activate_Effect()
    {
    }

    public override CardType Type => Effect?.ActivatedEffectCardType ?? CardType.Skill;

    public override TargetType TargetType => Effect?.ActivatedEffectTarget ?? TargetType.Self;

    protected override int CanonicalEnergyCost => Effect?.ActivatedEffectEnergyCost ?? 0;

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
            return !MonsterCommandRegistry.GetOrCreate(pet).HasUsedActivatedEffectThisTurn;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || SourceMonster is not IMonsterActivatedEffect impl || SourceMonster.FaceDown)
            return;

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
        return impl.IsActivatedEffectAvailable;
    }
}
