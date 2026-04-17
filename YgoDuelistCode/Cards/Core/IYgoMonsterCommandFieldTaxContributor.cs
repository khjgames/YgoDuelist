namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Face-up spell/trap zone contributor for monster-command taxes and repeat-count effects.
/// </summary>
public interface IYgoMonsterCommandFieldTaxContributor
{
    bool IsMonsterCommandFieldTaxActive();
    int GetMonsterCommandEnergyAdd();
    int GetMonsterCommandResolutionAdd();
    int GetMonsterCommandLifePaymentDivisor();
}
