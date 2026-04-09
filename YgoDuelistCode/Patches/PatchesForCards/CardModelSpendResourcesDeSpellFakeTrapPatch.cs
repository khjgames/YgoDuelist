using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Patches;

/// <summary>After paying for a card, refund 1 Energy if De-Spell / Fake Trap discount applies, then remove the power.</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
public static class CardModelSpendResourcesDeSpellFakeTrapPatch
{
    public static void Postfix(Task __result, CardModel __instance)
    {
        _ = RefundAfterSpendAsync(__result, __instance);
    }

    private static async Task RefundAfterSpendAsync(Task spendTask, CardModel card)
    {
        await spendTask;

        Player? player = card.Owner;
        if (player?.Creature == null)
            return;

        if (card is BaseSpellCard && player.Creature.GetPower<DeSpellNextSpellDiscountPower>() is { } deSpell)
        {
            await PlayerCmd.GainEnergy(1, player);
            await PowerCmd.Remove(deSpell);
            return;
        }

        if (card is BaseTrapCard && player.Creature.GetPower<FakeTrapNextTrapDiscountPower>() is { } fakeTrap)
        {
            await PlayerCmd.GainEnergy(1, player);
            await PowerCmd.Remove(fakeTrap);
        }
    }
}
