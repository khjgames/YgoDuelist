namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Face-up spell/trap zone card contribution used by generic post-spell damage hooks.
/// </summary>
public interface IYgoSpellResolvedOwnerDamageContributor
{
    decimal GetOwnerSpellResolvedDamageAmount();
}
