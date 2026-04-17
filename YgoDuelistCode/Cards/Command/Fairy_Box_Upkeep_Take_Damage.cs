using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>Fairy Box upkeep option: take blockable damage (trap <c>Mgc2</c>). Display-only (choose-a-card UI).</summary>
public sealed class Fairy_Box_Upkeep_Take_Damage : MonsterCommandCard, IYgoFairyBoxUpkeepTakeDamageCommand
{
    public Fairy_Box_Upkeep_Take_Damage()
    {
    }

    public Fairy_Box_Upkeep_Take_Damage(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    public override bool UseAlternateUpgradedDescription => true;

    protected internal override string? CustomCommandEnergyTexturePath =>
        BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

    public override string PortraitPath => "fairy_box.png".CardImagePath();
}
