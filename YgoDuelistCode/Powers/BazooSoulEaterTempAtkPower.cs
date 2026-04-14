using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Temporary ATK from <see cref="Cards.Monster.Todo.Effect.Bazoo_the_Soul_Eater"/> banish effect; cleared at end of your turn.</summary>
public sealed class BazooSoulEaterTempAtkPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-BAZOO_SOUL_EATER_TEMP_ATK_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-BAZOO_SOUL_EATER_TEMP_ATK_POWER.description");

    protected override string? CardPortraitStemOverride => "bazoo_the_soul_eater";
}
