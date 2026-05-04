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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Gray_Wing : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString HandPrompt = new("cards", "YGODUELIST-GRAY_WING.hand_select");

    public Gray_Wing()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 13,
            baseDef: 7,
            baseMgc: 6,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Dragon | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Gray_Wing) };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GRAY_WING.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && YgoPlayerPiles.Hand(Owner)?.Cards.Count > 0;

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner?.PlayerCombatState == null)
            return base.GetSecondaryStats();
        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(p, this))
                continue;
            if (MonsterCommandRegistry.TryGet(p, out var s) && s.GrayWingAtkPenaltyThisTurn > 0)
                return (-s.GrayWingAtkPenaltyThisTurn, 0);
        }

        return base.GetSecondaryStats();
    }

    protected override int GetAttackDefendResolutionCount(Player? player)
    {
        int n = base.GetAttackDefendResolutionCount(player);
        if (Owner?.PlayerCombatState == null)
            return n;
        foreach (Creature p in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(p, this))
                continue;
            if (MonsterCommandRegistry.TryGet(p, out var s) && s.GrayWingDoubleAttackThisTurn)
                return n + 1;
        }

        return n;
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Gray_Wing gw || Owner?.Creature == null)
            return;

        Player player = Owner;
        CardModel? chosen = await YgoHandCardSelection.TryChooseSingleHandCardAsync<CardModel>(
            choiceContext,
            player,
            new CardSelectorPrefs(HandPrompt, 1, 1) { Cancelable = true },
            excludeReference: gw);
        if (chosen == null)
            return;

        CardPile? discard = YgoPlayerPiles.Discard(player);
        if (discard == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, discard, CardPilePosition.Top, chosen, false);

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(gw, player);
        if (pet == null)
            return;

        var state = MonsterCommandRegistry.GetOrCreate(pet);
        state.GrayWingAtkPenaltyThisTurn = (int)gw.DynamicVars["Mgc"].BaseValue;
        state.GrayWingDoubleAttackThisTurn = true;
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
