using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace YgoDuelist.YgoDuelistCode.Localization.DynamicVars;

/// <summary>
/// A dynamic var whose displayed value is computed each time it's read (e.g. from hand count).
/// Use for card text preview so values update when hand changes.
/// When there is no owner (e.g. deck/compendium view), <paramref name="valueWhenNoOwner"/> is used.
/// </summary>
public sealed class ComputedDecimalVar : DynamicVar
{
    private readonly Func<CardModel, decimal> _getter;
    private readonly decimal _valueWhenNoOwner;

    public ComputedDecimalVar(string name, Func<CardModel, decimal> getter, decimal valueWhenNoOwner = 0m)
        : base(name, 0m)
    {
        _getter = getter;
        _valueWhenNoOwner = valueWhenNoOwner;
    }

    protected override decimal GetBaseValueForIConvertible()
    {
        if (_owner is CardModel card)
            return _getter(card);
        return _valueWhenNoOwner;
    }

    public override string ToString()
    {
        return ((int)GetBaseValueForIConvertible()).ToString();
    }
}
