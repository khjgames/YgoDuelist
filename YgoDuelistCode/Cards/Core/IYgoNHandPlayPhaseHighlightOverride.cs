using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Combat play-phase outline on <see cref="MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder"/>:
/// return a modulate to replace vanilla cyan playable glow. Wired by <c>YgoFusionGateFieldGlowPatch</c>.
/// </summary>
public interface IYgoNHandPlayPhaseHighlightOverride
{
    /// <param name="vanillaWouldUseCyanPlayableHighlight">True when vanilla would use cyan (not red/gold).</param>
    /// <returns>Null to keep vanilla highlight behavior for this frame.</returns>
    Color? GetNHandPlayPhaseHighlightModulateOverride(NHandCardHolder holder, bool vanillaWouldUseCyanPlayableHighlight);
}

/// <summary>Shared modulate for fusion-style purple highlights.</summary>
public static class YgoNHandPlayPhaseHighlightColors
{
    public static readonly Color FusionStylePurple = new(0.78f, 0.42f, 1f, 0.98f);
}
