using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared helpers for creating or reusing blocking player-choice contexts.
/// </summary>
public static class YgoChoiceContexts
{
    public static BlockingPlayerChoiceContext Blocking(PlayerChoiceContext? choiceContext = null) =>
        choiceContext as BlockingPlayerChoiceContext ?? new BlockingPlayerChoiceContext();
}
