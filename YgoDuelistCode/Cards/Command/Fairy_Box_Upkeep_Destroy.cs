using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Extensions;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>Fairy Box upkeep option: destroy Fairy Box. Display-only (choose-a-card UI).</summary>
public sealed class Fairy_Box_Upkeep_Destroy : MonsterCommandCard, IYgoFairyBoxUpkeepDestroyTrapCommand
{
    public Fairy_Box_Upkeep_Destroy()
    {
    }

    public Fairy_Box_Upkeep_Destroy(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    protected internal override string? CustomCommandEnergyTexturePath =>
        BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

    public override string PortraitPath => "fairy_box.png".CardImagePath();
}
