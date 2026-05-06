using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class DnaFieldOverrideSync
{
    public static async Task SyncCombatFieldOverridesAsync(Player triggerPlayer)
    {
        if (triggerPlayer?.Creature?.CombatState == null)
            return;

        var players = YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(triggerPlayer.Creature.CombatState.Players);
        int? raceOverride = ResolveDeclaredRaceOverride(players);
        int? attributeOverride = ResolveDeclaredAttributeOverride(players);

        foreach (Player player in players)
        {
            if (player.PlayerCombatState == null)
                continue;
            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
            {
                if (!pet.IsAlive)
                    continue;
                if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard source)
                    continue;

                bool faceUpFieldMonster = !source.FaceDown;
                await SyncCounterPowerAmountAsync<DnaSurgeryRaceOverridePower>(
                    pet,
                    faceUpFieldMonster && raceOverride.HasValue ? raceOverride.Value + 1 : 0,
                    triggerPlayer);
                await SyncCounterPowerAmountAsync<DnaTransplantAttributeOverridePower>(
                    pet,
                    faceUpFieldMonster && attributeOverride.HasValue ? attributeOverride.Value + 1 : 0,
                    triggerPlayer);
            }
        }

        foreach (Player player in players)
            await LevelModifierPowerSync.SyncFieldLevelModifierPowersAndHpAsync(player);
    }

    private static int? ResolveDeclaredRaceOverride(System.Collections.Generic.IReadOnlyList<Player> players)
    {
        int? result = null;
        foreach (Player player in players)
        {
            CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
            if (zone == null)
                continue;
            foreach (CardModel card in YgoMpCombatOrder.CardsSnapshotOrderedForMp(zone.Cards))
            {
                if (card is DNA_Surgery dna && !dna.FaceDown && dna.DeclaredRace.HasValue)
                    result = (int)dna.DeclaredRace.Value;
            }
        }

        return result;
    }

    private static int? ResolveDeclaredAttributeOverride(System.Collections.Generic.IReadOnlyList<Player> players)
    {
        int? result = null;
        foreach (Player player in players)
        {
            CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
            if (zone == null)
                continue;
            foreach (CardModel card in YgoMpCombatOrder.CardsSnapshotOrderedForMp(zone.Cards))
            {
                if (card is DNA_Transplant dna && !dna.FaceDown && dna.DeclaredAttribute.HasValue)
                    result = (int)dna.DeclaredAttribute.Value;
            }
        }

        return result;
    }

    private static async Task SyncCounterPowerAmountAsync<TPower>(Creature target, int desiredAmount, Player applier)
        where TPower : MegaCrit.Sts2.Core.Models.PowerModel
    {
        TPower? existing = target.GetPower<TPower>();
        if (desiredAmount <= 0)
        {
            if (existing != null)
                await PowerCmd.Remove<TPower>(target);
            return;
        }

        if (existing == null)
        {
            await PowerCmd.Apply<TPower>(target, desiredAmount, applier.Creature, null);
            return;
        }

        int delta = desiredAmount - (int)existing.Amount;
        if (delta != 0)
            await PowerCmd.ModifyAmount(existing, delta, applier.Creature, null);
    }
}
