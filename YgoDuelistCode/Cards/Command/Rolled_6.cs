using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Display-only d6 result. No play or option-pile click behavior.
/// </summary>
public sealed class Rolled_6 : MonsterCommandCard
{
    public Rolled_6()
    {
    }

    public Rolled_6(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    protected internal override string? CustomCommandEnergyTexturePath =>
        BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

    public override string PortraitPath => "YgoDuelist/images/card_frames/Rolled_6.png";
}
