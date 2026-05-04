using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class The_Kick_Man : EffectMonsterCard
{
    public The_Kick_Man()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 13,
            baseDef: 3,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Dark | YgoCardPackTags.Zombie;
    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.OnAfterMonsterPlayResolved(choiceContext, cardPlay);
        await TryEquipFromGraveyardAsync(choiceContext);
    }

    private async Task TryEquipFromGraveyardAsync(PlayerChoiceContext choiceContext)
    {
        if (Owner?.PlayerCombatState == null)
            return;
        if (ColdWaveSpellTrapLockGate.IsPlayerLockedThisTurn(Owner))
            return;

        var field = DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner);
        if (field == null || !field.Contains(this))
            return;
        List<BaseEquipSpellCard> candidates = BuildEquipCandidates(Owner, this);

        if (candidates.Count == 0)
            return;

        if (!YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(Owner, candidates[0]))
            return;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = false,
            Cancelable = true
        };

        BaseEquipSpellCard? equip = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            prefs,
            () => BuildEquipCandidates(Owner, this));
        if (equip == null)
            return;

        CardPile? gy = YgoPlayerPiles.Graveyard(Owner);
        if (gy == null)
            return;

        if (equip.Pile?.Type != GraveyardPile.CustomType || !gy.Cards.Contains(equip))
            return;
        if (!YgoEquipSpellTargetRules.IsLegalEquipTarget(equip, this))
            return;
        if (FairyOfSpringReturnedEquipLock.IsLocked(equip))
            return;
        if (!YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(Owner, equip))
            return;
        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(Owner, this))
            return;

        ColdWaveSpellTrapLockGate.MarkPlayerUsedSpellTrapThisTurn(equip.Owner);
        await YgoSpellTrapZoneBridge.ActivateEquipSpellAsync(equip, this);
        await YgoCurseOfDarknessSpellHook.AfterSpellResolved(choiceContext, equip);
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(Owner);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandRepublishIfZoneViewActive(Owner);
    }

    private static List<BaseEquipSpellCard> BuildEquipCandidates(Player player, The_Kick_Man target)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        return gy == null
            ? []
            : YgoMpCombatOrder.CardsSnapshotOrderedForMp(gy.Cards)
                .OfType<BaseEquipSpellCard>()
                .Where(e => !e.FaceDown && !FairyOfSpringReturnedEquipLock.IsLocked(e) && YgoEquipSpellTargetRules.IsLegalEquipTarget(e, target))
                .ToList();
    }
}
