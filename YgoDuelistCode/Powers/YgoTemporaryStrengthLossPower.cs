using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Uses base-game temporary Strength-down behavior (TemporaryStrengthPower / Regent-style):
/// apply negative Strength now, then automatically restore at end of owner's side turn.
/// </summary>
public sealed class YgoTemporaryStrengthLossPower : TemporaryStrengthPower
{
    private AbstractModel? _origin;

    public override AbstractModel OriginModel => _origin ?? ModelDb.Card<Stumbling>();

    protected override bool IsPositive => false;

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // Preserve source hover info when available; fallback keeps this deterministic.
        _origin = cardSource ?? ModelDb.Card<Stumbling>();
        await base.BeforeApplied(target, amount, applier, cardSource);
    }
}

