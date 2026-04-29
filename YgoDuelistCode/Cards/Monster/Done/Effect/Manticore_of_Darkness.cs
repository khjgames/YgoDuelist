using System;
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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>GY end-phase Special Summon — <see cref="YgoDuelist.YgoDuelistCode.Services.YgoManticoreOfDarknessEndPhase"/>.</summary>
public sealed class Manticore_of_Darkness : EffectMonsterCard, IYgoOwnerBeforeTurnEndFlushGraveyardEffect
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-MANTICORE_OF_DARKNESS.activate_effect");
    private static readonly LocString FodderPrompt = new("cards", "YGODUELIST-MANTICORE_OF_DARKNESS.select_fodder");
    private int _sentToGraveyardOwnerTurnStamp = -1;

    public Manticore_of_Darkness()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 23,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Fire | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Manticore_of_Darkness) };

    public override void OnAfterPileMoveCompleted(Player? player, PileType? from, PileType newPileType)
    {
        if (player != null && from != GraveyardPile.CustomType && newPileType == GraveyardPile.CustomType)
            _sentToGraveyardOwnerTurnStamp = YgoPlayerCombatTurnStamp.Get(player);
    }

    public bool IsOwnerBeforeTurnEndFlushGraveyardEffectActive() =>
        Owner != null && _sentToGraveyardOwnerTurnStamp == YgoPlayerCombatTurnStamp.Get(Owner);

    public async Task TryResolveOwnerBeforeTurnEndFlushGraveyardEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(owner);
        if (gy == null || !gy.Cards.Contains(this))
            return;
        if (!IsOwnerBeforeTurnEndFlushGraveyardEffectActive())
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(owner, 0))
            return;

        List<CardModel> fodder = BuildFodderCandidates(owner);
        if (fodder.Count == 0)
            return;

        var activatePrefs = new CardSelectorPrefs(ActivatePrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };
        Manticore_of_Darkness? activationPick = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            owner,
            activatePrefs,
            () => new List<Manticore_of_Darkness> { this });
        if (!ReferenceEquals(activationPick, this))
            return;

        fodder = BuildFodderCandidates(owner);
        if (fodder.Count == 0)
            return;

        BaseMonsterCard? food = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            owner,
            new CardSelectorPrefs(FodderPrompt, 1, 1) { Cancelable = true },
            () => BuildFodderCandidates(owner).OfType<BaseMonsterCard>().ToList());
        if (food == null)
            return;
        if (!await SendMonsterToGraveyardAsync(owner, food))
            return;
        if (!gy.Cards.Contains(this))
            return;
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(owner, this, choiceContext);
    }

    private List<CardModel> BuildFodderCandidates(Player player)
    {
        var list = new List<CardModel>();
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand != null)
        {
            foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(hand.Cards))
            {
                if (c is BaseMonsterCard bm && IsFodderRace(bm.DuelMonsterRace))
                    list.Add(bm);
            }
        }

        foreach (BaseMonsterCard field in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
        {
            if (IsFodderRace(field.DuelMonsterRace))
                list.Add(field);
        }

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(list);
    }

    private static bool IsFodderRace(DuelMonsterRace r) =>
        r == DuelMonsterRace.Beast || r == DuelMonsterRace.BeastWarrior || r == DuelMonsterRace.WingedBeast;

    private static async Task<bool> SendMonsterToGraveyardAsync(Player player, BaseMonsterCard m)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return false;
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand != null && m.Pile == hand)
        {
            await CardPileCmd.Add(new[] { m }, gy, CardPilePosition.Top, m, false);
            return true;
        }
        if (m is not NormalMonsterCard nm)
            return false;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(nm, player);
        if (pet == null)
            return false;
        await CreatureCmd.Kill(pet, force: true);
        await CardPileCmd.Add(new[] { m }, gy, CardPilePosition.Top, m, false);
        return true;
    }
}
