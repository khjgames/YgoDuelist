using System.Threading.Tasks;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Spell/trap cards in the zone that react when any duel monster leaves the field (after field registries update).</summary>
public interface IYgoAfterDuelMonsterDiedZoneCard
{
    Task AfterDuelMonsterDiedAsync(DuelMonsterPetDeathContext ctx);
}
