using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// When a face-down duel monster on the field becomes face-up (flip), <see cref="OnFlippedFaceUpAsync"/> runs.
/// </summary>
public interface IMonsterFlipEffect
{
    Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self);
}
