using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// <see cref="Giant_Germ"/>: when destroyed by battle and sent to the Graveyard, inflict <c>Mgc</c> Blight on all enemies,
/// then optionally Special Summon up to 2 <see cref="Giant_Germ"/> from the deck.
/// </summary>
public static class YgoGiantGermGraveyard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-GIANT_GERM.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-GIANT_GERM.summon_from_deck");

    public static void OnCardAddedToGraveyardPile(CardPile pile, CardModel addedCard)
    {
        if (addedCard is not Giant_Germ germ)
            return;
        if (!YgoGiantGermBattleDeathGate.Consume(germ))
            return;
        if (pile.Type != GraveyardPile.CustomType || !pile.IsCombatPile)
            return;
        if (CombatManager.Instance is not { IsInProgress: true })
            return;
        CombatState? cs = CombatManager.Instance.DebugOnlyGetState();
        if (cs == null)
            return;

        Player? player = ResolveGraveyardOwner(cs, pile) ?? addedCard.Owner;
        if (player?.Creature?.CombatState == null || player.Creature.Side != CombatSide.Player)
            return;

        TaskHelper.RunSafely(RunAsync(player, germ));
    }

    private static Player? ResolveGraveyardOwner(CombatState cs, CardPile pile)
    {
        foreach (Player p in cs.Players)
        {
            if (GraveyardRelic.GetGraveyardPile(p) == pile)
                return p;
        }

        return null;
    }

    private static List<Giant_Germ> CollectGermsInDeck(Player player)
    {
        CardPile? draw = PileType.Draw.GetPile(player);
        if (draw == null)
            return [];

        return draw.Cards.OfType<Giant_Germ>().Where(g => g.CanSummonDuelMonster).ToList();
    }

    private static async Task RunAsync(Player player, Giant_Germ sourceInGraveyard)
    {
        int blight = (int)sourceInGraveyard.DynamicVars["Mgc"].BaseValue;
        if (blight > 0 && player.Creature?.CombatState != null)
        {
            foreach (Creature enemy in player.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive))
                await PowerCmd.Apply<BlightPower>(enemy, blight, player.Creature, sourceInGraveyard);
        }

        var ctx = new BlockingPlayerChoiceContext();

        var activatePrefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> activationPick = await CardSelectCmd.FromSimpleGrid(
            ctx,
            new[] { sourceInGraveyard },
            player,
            activatePrefs);

        if (activationPick.FirstOrDefault() is not Giant_Germ)
            return;

        for (int i = 0; i < 2; i++)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                return;

            List<Giant_Germ> germs = CollectGermsInDeck(player);
            if (germs.Count == 0)
                return;

            var summonPrefs = new CardSelectorPrefs(SummonPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            };

            IEnumerable<CardModel> summonPick = await CardSelectCmd.FromSimpleGrid(ctx, germs, player, summonPrefs);
            if (summonPick.FirstOrDefault() is not Giant_Germ chosen)
                return;

            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
        }
    }
}
