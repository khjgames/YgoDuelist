namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Face-up spell/trap in zone enables draw-pile preview when <see cref="Spell.Todo.Continuos.Convulsion_of_Nature"/> rules apply.
/// </summary>
public interface IYgoConvulsionDrawPilePreviewSource
{
    bool IsFaceUpActiveForConvulsionDrawPreview();
}
