namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Source monster for <see cref="Command.Command_Defend"/> that grants delayed block after resolution (e.g. Total Defense Shogun).
/// </summary>
public interface IYgoDeferredBlockFromDefendCommand
{
    int GetDeferredBlockForDefendCommand();
}
