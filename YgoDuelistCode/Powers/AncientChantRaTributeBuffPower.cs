using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Banishing Ancient Chant from the Graveyard: your next Tribute Summon of The Winged Dragon of Ra this turn uses combined original ATK/DEF of tributes as its printed ATK/DEF.
/// </summary>
public sealed class AncientChantRaTributeBuffPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => new("powers", "YGODUELIST-ANCIENT_CHANT_RA_TRIBUTE_BUFF_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-ANCIENT_CHANT_RA_TRIBUTE_BUFF_POWER.description");
}
