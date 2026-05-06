using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Second field command on a monster (see <see cref="Command.Activate_Effect_2"/>). Separate once-per-turn slot from <see cref="IMonsterActivatedEffect"/>.
/// </summary>
public interface IMonsterSecondActivatedEffect
{
    int SecondActivatedEffectEnergyCost { get; }
    CardType SecondActivatedEffectCardType { get; }
    TargetType SecondActivatedEffectTarget { get; }

    string SecondActivatedEffectDescriptionLocKey { get; }

    bool IsSecondActivatedEffectAvailable => true;

    bool SecondActivatedEffectConsumesOncePerTurnSlot => true;

    /// <summary>
    /// When non-null, <see cref="Command.Activate_Effect_2"/> uses this portrait (e.g. the card the effect summons or sets).
    /// Default: same art as the field monster (<see cref="MonsterCommandCard.SourceMonster"/>).
    /// </summary>
    string? SecondActivatedEffectPortraitPath => null;

    Task OnSecondActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source);
}
