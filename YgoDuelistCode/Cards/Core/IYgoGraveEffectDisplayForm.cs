using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

public interface IYgoGraveEffectDisplayForm
{
    bool SupportsGraveEffectDisplayForm { get; }
    bool IsGraveEffectDisplayFormActive { get; }

    void ToggleGraveEffectDisplayForm(bool allowCanonicalUiPreview = false);
    void CopyGraveEffectDisplayFormFrom(IYgoGraveEffectDisplayForm source);

    LocString GetGraveEffectDescriptionLocString();
}