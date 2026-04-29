using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Solo <see cref="Cards.Monster.Done.Effect.Morphing_Jar_2"/> FLIP: return all your field monsters to the bottom of the
/// deck, shuffle, excavate until N monsters seen (N = field count), then Special Summon each Level 4 or lower monster
/// among excavated cards in face-down Defense; send the rest to the Graveyard.
/// </summary>
public static class YgoMorphingJar2SoloFlip
{
    private static readonly LocString FlipRevealPreviewPrompt =
        new("cards", "YGODUELIST-MORPHING_JAR_2.flip_preview.selection");

    public static async Task RunAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.PlayerCombatState == null)
            return;

        var pcs = player.PlayerCombatState;
        CardPile draw = pcs.DrawPile;
        CardPile discard = pcs.DiscardPile;
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return;

        List<BaseMonsterCard> fieldSnapshot = DuelMonsterFieldRegistry.OrderedFieldMonsters(player);
        int n = fieldSnapshot.Count;

        foreach (BaseMonsterCard fieldCard in fieldSnapshot)
        {
            Creature? pet = YgoMpCombatOrder.FirstPetWhere(
                pcs,
                p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, fieldCard));
            if (pet == null)
                continue;

            await DuelMonsterPetDeathPatch.ReleaseLiveFieldMonsterToDrawPileAsync(player, pet, fieldCard, draw, gy);
        }

        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        var revealed = new List<CardModel>();
        int monstersSeen = 0;
        while (monstersSeen < n && !draw.IsEmpty)
        {
            await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
            if (draw.IsEmpty)
                break;

            CardModel? top = draw.Cards.Count > 0 ? draw.Cards[0] : null;
            if (top == null)
                break;

            revealed.Add(top);
            if (top is BaseMonsterCard)
                monstersSeen++;

            await CardPileCmd.Add(top, discard, CardPilePosition.Top, top, false);
        }

        if (revealed.Count > 0)
        {
            var prefs = new CardSelectorPrefs(FlipRevealPreviewPrompt, 0, 0)
            {
                RequireManualConfirmation = true,
                Cancelable = false
            };
            try
            {
                YgoMonsterFormPreviewContext.RestrictMonsterToggleToAttackDefenseOnly = true;
                await YgoPreviewGridSelection.ShowPreviewAsync(choiceContext, revealed, player, prefs);
            }
            finally
            {
                YgoMonsterFormPreviewContext.RestrictMonsterToggleToAttackDefenseOnly = false;
            }
        }

        foreach (CardModel card in revealed)
        {
            if (card is BaseMonsterCard bm
                && bm.DuelMonsterLevel <= 4
                && bm.CanSummonDuelMonster
                && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            {
                bm.FaceDown = true;
                bool summoned = await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, bm, choiceContext);
                if (!summoned && card.Pile != gy)
                    await CardPileCmd.Add(card, gy, CardPilePosition.Top, card, false);
            }
            else if (card.Pile != gy)
                await CardPileCmd.Add(card, gy, CardPilePosition.Top, card, false);
        }
    }
}
