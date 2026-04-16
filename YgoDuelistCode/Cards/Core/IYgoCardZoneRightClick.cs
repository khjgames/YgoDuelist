using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

[System.Flags]
public enum YgoCardRightClickActivation
{
    None = 0,
    SpellTrapZoneFaceUp = 1 << 0,
    OptionPileFaceUp = 1 << 1,
}

/// <summary>
/// Right-click handling for cards shown in the spell/trap second hand or option row. The patch matches
/// <see cref="RightClickActivationMask"/> against <see cref="MegaCrit.Sts2.Core.Entities.Cards.CardModel.Pile"/> / face state before calling the handler.
/// </summary>
public interface IYgoCardZoneRightClick
{
    YgoCardRightClickActivation RightClickActivationMask { get; }

    /// <summary>Returns true if the click was consumed (including when a flow is already active).</summary>
    bool TryHandleCardZoneRightClick(NHandCardHolder holder);
}
