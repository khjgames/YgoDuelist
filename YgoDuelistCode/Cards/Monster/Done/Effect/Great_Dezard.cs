using System.Collections.Generic;
using System.Linq;
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
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Activate Effect (1 Energy, 1 Conduit): Tribute this card; destroy 1 monster in your hand; Special Summon 1 "Fushioh Richie" from your hand, draw pile, or discard pile.
/// </summary>
public sealed class Great_Dezard : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString DestroyHandMonsterPrompt =
        new("cards", "YGODUELIST-GREAT_DEZARD.destroy_hand_monster_select");

    private static readonly LocString SummonFushiohPrompt =
        new("cards", "YGODUELIST-GREAT_DEZARD.summon_fushioh_select");

    public Great_Dezard()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 19,
            baseDef: 23,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster;
    public override Type[] RelatedCards => new[] { typeof(Great_Dezard), typeof(Fushioh_Richie) };

    public int ActivatedEffectEnergyCost => 1;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GREAT_DEZARD.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner) != null
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 1)
        && CanResolveDezardEffect(Owner);

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = cardPlay;
        if (source is not Great_Dezard || Owner?.Creature == null)
            return;

        Player player = Owner;
        if (!CanResolveDezardEffect(player))
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, player);
        if (pet == null)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 1))
            return;

        List<Fushioh_Richie> fushCandidates = BuildFushiohHandOrDrawCandidates(player);
        if (fushCandidates.Count == 0)
            return;

        Fushioh_Richie? fush = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(SummonFushiohPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildFushiohHandOrDrawCandidates(player));
        if (fush == null)
            return;

        List<BaseMonsterCard> handDestroyPool = BuildHandMonsterDestroyCandidates(player, fush);
        if (handDestroyPool.Count == 0)
            return;

        BaseMonsterCard? toDestroy = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(DestroyHandMonsterPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildHandMonsterDestroyCandidates(player, fush));
        if (toDestroy == null)
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (hand == null || gy == null || !hand.Cards.Contains(toDestroy))
            return;

        if (fush.Pile?.Type is not (PileType.Hand or PileType.Draw or PileType.Discard))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
        await CreatureCmd.Kill(pet, force: true);
        await CardPileCmd.Add(new[] { toDestroy }, gy, CardPilePosition.Top, toDestroy, false);

        YgoFushiohRichieSummonGate.Grant(player);
        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, fush, choiceContext))
            YgoFushiohRichieSummonGate.Consume(player);
    }

    private static bool CanResolveDezardEffect(Player player)
    {
        if (player.PlayerCombatState == null)
            return false;

        List<Fushioh_Richie> fush = BuildFushiohHandOrDrawCandidates(player);
        if (fush.Count == 0)
            return false;

        foreach (Fushioh_Richie f in fush)
        {
            if (BuildHandMonsterDestroyCandidates(player, f).Count > 0)
                return true;
        }

        return false;
    }

    private static List<Fushioh_Richie> BuildFushiohHandOrDrawCandidates(Player player) =>
        YgoPlayerPiles.OrderedCardsOfTypeFromHandDrawDiscard<Fushioh_Richie>(player);

    private static List<BaseMonsterCard> BuildHandMonsterDestroyCandidates(Player player, Fushioh_Richie chosenFush)
    {
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return new List<BaseMonsterCard>();

        return YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(hand.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m => !ReferenceEquals(m, chosenFush))
            .ToList();
    }
}
