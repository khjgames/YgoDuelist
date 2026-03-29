using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Temporary Strength loss from <see cref="Spellbinding_Circle"/> (Spellbinding).</summary>
public sealed class SpellbindingTemporaryStrengthPower : TemporaryStrengthPower
{
    private AbstractModel? _origin;

    public override AbstractModel OriginModel => _origin ?? ModelDb.Card<Spellbinding_Circle>();

    protected override bool IsPositive => false;

    public override LocString Title => new("powers", "YGODUELIST-SPELLBINDING_TEMPORARY_STRENGTH_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SPELLBINDING_TEMPORARY_STRENGTH_POWER.description");

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _origin = cardSource ?? ModelDb.Card<Spellbinding_Circle>();
        await base.BeforeApplied(target, amount, applier, cardSource);
    }
}
