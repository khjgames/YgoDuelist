using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Ephemeral cards for the d6 grid in <see cref="YgoBlindDestructionContinuous"/>.</summary>
public sealed class YgoDieFaceProxyCard : CustomCardModel
{
    public int Face { get; }

    public YgoDieFaceProxyCard()
        : this(1)
    {
    }

    public YgoDieFaceProxyCard(int face)
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, showInCardLibrary: false, autoAdd: false)
    {
        Face = face;
    }
}
