using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Display-only coin result (Tails). No play or option-pile click behavior.
/// </summary>
public sealed class Tails : MonsterCommandCard
{
    public Tails()
    {
    }

    public Tails(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    protected internal override string? CustomCommandEnergyTexturePath =>
        BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

    public override string PortraitPath => "YgoDuelist/images/card_frames/Tails.png";
}
