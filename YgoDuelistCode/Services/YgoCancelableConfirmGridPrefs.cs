using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Shared <see cref="CardSelectorPrefs"/> for pre-play pile/target grids: explicit confirm + cancel closes without spending cost.
/// </summary>
public static class YgoCancelableConfirmGridPrefs
{
    public static CardSelectorPrefs ForSinglePick(LocString prompt) =>
        new(prompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };
}
