using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>When destroyed and sent from the field to the Graveyard, you may Special Summon 1 Pyro monster from your hand.</summary>
public sealed class The_Thing_in_the_Crater : EffectMonsterCard
{
    private static readonly LocString SummonPyroPrompt = new("cards", "YGODUELIST-THE_THING_IN_THE_CRATER.summon_pyro_hand");

    public The_Thing_in_the_Crater()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 10,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Fire;

    public override Type[] RelatedCards => new[] { typeof(The_Thing_in_the_Crater) };

    public override Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        TaskHelper.RunSafely(TryOptionalSpecialSummonPyroFromHandAsync(ctx.Player));
        return base.OnPetDiedAfterOptionPileHandlingAsync(ctx);
    }

    private async Task TryOptionalSpecialSummonPyroFromHandAsync(Player player)
    {
        if (player?.Creature?.CombatState == null)
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        List<BaseMonsterCard> candidates = BuildHandPyroCandidates(player);
        if (candidates.Count == 0)
            return;

        var ctx = YgoChoiceContexts.Blocking();
        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(SummonPyroPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildHandPyroCandidates(player));
        if (chosen == null)
            return;
        if (chosen.Pile?.Type != PileType.Hand)
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
    }

    private static List<BaseMonsterCard> BuildHandPyroCandidates(Player player)
    {
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return new List<BaseMonsterCard>();

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m =>
                m.DuelMonsterRace == DuelMonsterRace.Pyro
                && (m.CanSummonDuelMonster || m.AllowSpecialSummonIgnoringCanSummonDuelMonsterGate))
            .ToList();
    }
}
