namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// <see cref="Command.Activate_Effect"/> / <see cref="Command.Activate_Effect_2"/> pile title/description for
/// <c>GetDescriptionForPile</c> / title patch — logic stays on the command card.
/// </summary>
public interface IActivateEffectPileUi
{
    bool TryGetActivateEffectPileDescription(ref string result);

    bool TryGetActivateEffectPileTitle(ref string result);
}
