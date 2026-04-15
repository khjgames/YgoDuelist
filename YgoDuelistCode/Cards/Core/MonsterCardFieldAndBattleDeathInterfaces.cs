using MegaCrit.Sts2.Core.Localization;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Field → GY: search main deck by printed ATK cap (e.g. Sangan). Logic lives in <see cref="Services.YgoFieldToGraveyardDeckSearch"/>.</summary>
public interface IFieldToGraveyardDeckSearchEffect
{
    LocString FieldToGraveyardSearchPrompt { get; }
    int FieldToGraveyardSearchMaxPrintedAtk { get; }
    bool FieldToGraveyardSearchApplyNameLock { get; }
}

/// <summary>Destroyed by battle → GY: optional activate, then Special Summon one matching monster from the deck.</summary>
public interface IBattleDeathOptionalDeckSpecialSummon
{
    LocString BattleDeathActivatePrompt { get; }
    LocString BattleDeathSummonPrompt { get; }
    bool IsBattleDeathDeckSummonCandidate(BaseMonsterCard m);
}

/// <summary>Sent to GY (no battle-death mark): optional activate, then Special Summon one matching monster from the deck — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoGraveyardOptionalDeckSpecialSummon"/>.</summary>
public interface IGraveyardOptionalDeckSpecialSummon
{
    LocString GraveyardActivatePrompt { get; }
    LocString GraveyardSummonPrompt { get; }
    bool IsGraveyardDeckSummonCandidate(BaseMonsterCard m);
}
