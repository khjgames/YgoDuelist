using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>When an Equip Spell attaches to <see cref="Gearfried_the_Iron_Knight"/>, destroy that equip, gain 1 Energy, add Mgc stacks of <see cref="GearfriedIronKnightPower"/>.</summary>
public static class YgoGearfriedEquipReaction
{
    public static async Task TryReactAfterEquipAttachedAsync(Player player, BaseEquipSpellCard equip, BaseMonsterCard targetMonster)
    {
        if (targetMonster is not Gearfried_the_Iron_Knight gearfried)
            return;

        if (player.PlayerCombatState == null)
            return;

        Creature? pet = player.PlayerCombatState.Pets
            .FirstOrDefault(p => p.IsAlive && DuelMonsterFieldRegistry.GetSourceCardForPet(p) == gearfried);
        if (pet == null)
            return;

        YgoEquipSpellRegistry.Detach(equip);

        CardPile? gy = GraveyardPile.CustomType.GetPile(player);
        if (gy != null)
            await CardPileCmd.Add(new[] { equip }, gy, CardPilePosition.Top, equip, false);

        await PlayerCmd.GainEnergy(1, player);

        decimal add = gearfried.DynamicVars["Mgc"].BaseValue;
        if (add <= 0m)
            return;

        GearfriedIronKnightPower? existing = pet.GetPower<GearfriedIronKnightPower>();
        if (existing != null)
            await PowerCmd.ModifyAmount(existing, add, player.Creature, gearfried);
        else
            await PowerCmd.Apply<GearfriedIronKnightPower>(pet, add, player.Creature, gearfried);
    }
}
