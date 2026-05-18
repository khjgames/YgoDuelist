using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Prevents two Blight pulses on the same owner-turn stamp for the same pet when
/// <see cref="Cards.Monster.Done.Effect.Solar_Flare_Dragon.OnAfterSummonPipelineAsync"/> runs during
/// <see cref="Relics.GraveyardRelic.AfterPlayerTurnStart"/> before <see cref="YgoOwnerTurnStartFieldMonsterHooks.TryResolvePlayerTurnStart"/>.
/// </summary>
public static class YgoSolarFlareDragonTurnPulseDedup
{
    private static readonly Dictionary<uint, int> LastPulseStampByPetCombatId = new();

    public static bool AlreadyPulsedThisOwnerTurn(Player player, Creature pet)
    {
        if (player == null || pet == null || pet.CombatId is not uint combatId)
            return false;
        int stamp = YgoPlayerCombatTurnStamp.Get(player);
        return LastPulseStampByPetCombatId.TryGetValue(combatId, out int last) && last == stamp;
    }

    public static void NotePulse(Player player, Creature pet)
    {
        if (player == null || pet == null || pet.CombatId is not uint combatId)
            return;
        LastPulseStampByPetCombatId[combatId] = YgoPlayerCombatTurnStamp.Get(player);
    }

    public static void ClearAll() => LastPulseStampByPetCombatId.Clear();
}
