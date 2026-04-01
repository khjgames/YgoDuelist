using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YgoDuelist.YgoDuelistCode;

/// <summary>
/// Custom <see cref="CardTag"/> values for preview / tooltip shells (BaseLib <see cref="CustomEnumAttribute"/>).
/// </summary>
public static class YgoDuelistPreviews
{
    [CustomEnum]
    public static CardTag PreviewEffect;
}
