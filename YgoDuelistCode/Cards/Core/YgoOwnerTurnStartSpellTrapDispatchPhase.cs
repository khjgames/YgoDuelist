namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// When owner turn-start spell/trap zone hooks run relative to other <see cref="GraveyardRelic.AfterPlayerTurnStart"/> work.
/// </summary>
public enum YgoOwnerTurnStartSpellTrapDispatchPhase
{
    /// <summary>Before field-pet and in-graveyard owner-turn-start hooks (e.g. Sanctuary mercury draw).</summary>
    BeforeOwnerFieldPetHooks,

    /// <summary>After Sealmaster talisman gate cleanup, before field-monster turn-start hooks.</summary>
    AfterSealmasterBeforeFieldMonsterHooks,
}
