using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>End-phase GY revive — <see cref="YgoTwinHeadedBehemothEndPhase"/>; field→GY tracking — <see cref="Patches.CardPileCmdFieldMonsterGraveyardEffectsPatch"/>.</summary>
public sealed class Twin_Headed_Behemoth : EffectMonsterCard, IYgoOwnerBeforeTurnEndFlushGraveyardEffect
{
    public override int AttackPortionCount => 2;

    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-TWIN_HEADED_BEHEMOTH.activate_revive");
    private int _gyReviveEligibleStamp = -1;

    public Twin_Headed_Behemoth()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 15,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Dragon;
    public override Type[] RelatedCards => new[] { typeof(Twin_Headed_Behemoth) };

    internal void MarkEndPhaseReviveEligible(int ownerTurnStamp) => _gyReviveEligibleStamp = ownerTurnStamp;

    internal bool IsEndPhaseReviveEligible(int ownerTurnStamp) =>
        _gyReviveEligibleStamp >= 0 && _gyReviveEligibleStamp == ownerTurnStamp;

    internal void ClearEndPhaseReviveEligibility() => _gyReviveEligibleStamp = -1;

    internal void ArmReviveSummonMiniStats()
    {
        if (DynamicVars?.Damage != null)
            DynamicVars.Damage.BaseValue = 10;
        if (DynamicVars != null && DynamicVars.ContainsKey("Def"))
            DynamicVars["Def"].BaseValue = 10;
    }

    internal void ClearReviveMiniStats()
    {
        if (DynamicVars?.Damage != null)
            DynamicVars.Damage.BaseValue = BaseAtk;
        if (DynamicVars != null && DynamicVars.ContainsKey("Def"))
            DynamicVars["Def"].BaseValue = BaseDef;
    }

    public override void OnAfterPileMoveCompleted(Player? player, PileType? from, PileType newPileType)
    {
        if (from == MonsterPile.CustomType && newPileType != MonsterPile.CustomType)
            ClearReviveMiniStats();
        if (player != null && from == MonsterPile.CustomType && newPileType == GraveyardPile.CustomType)
            MarkEndPhaseReviveEligible(YgoPlayerCombatTurnStamp.Get(player));
    }

    public bool IsOwnerBeforeTurnEndFlushGraveyardEffectActive() =>
        Owner != null && IsEndPhaseReviveEligible(YgoPlayerCombatTurnStamp.Get(Owner));

    public async Task TryResolveOwnerBeforeTurnEndFlushGraveyardEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        CardPile? gy = YgoPlayerPiles.Graveyard(owner);
        if (gy == null || !gy.Cards.Contains(this))
            return;
        int stamp = YgoPlayerCombatTurnStamp.Get(owner);
        if (!IsEndPhaseReviveEligible(stamp))
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(owner, 0))
            return;

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            owner,
            this,
            ActivatePrompt);
        if (ctx == null)
            return;
        if (!gy.Cards.Contains(this))
            return;
        ArmReviveSummonMiniStats();
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(owner, this, ctx);
        ClearEndPhaseReviveEligibility();
    }
}
