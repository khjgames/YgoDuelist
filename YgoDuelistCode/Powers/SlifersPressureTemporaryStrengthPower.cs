using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Temporary Strength loss from <see cref="Slifer_the_Sky_Dragon"/> (Slifer's Pressure).</summary>
public sealed class SlifersPressureTemporaryStrengthPower : TemporaryStrengthPower
{
    private AbstractModel? _origin;

    public override AbstractModel OriginModel => _origin ?? ModelDb.Card<Slifer_the_Sky_Dragon>();

    protected override bool IsPositive => false;

    public override LocString Title => new("powers", "YGODUELIST-SLIFERS_PRESSURE_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SLIFERS_PRESSURE_POWER.description");

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _origin = cardSource ?? ModelDb.Card<Slifer_the_Sky_Dragon>();
        await base.BeforeApplied(target, amount, applier, cardSource);
    }
}
