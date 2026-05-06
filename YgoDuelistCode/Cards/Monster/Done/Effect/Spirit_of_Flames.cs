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

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 1 FIRE from your Graveyard to Special Summon.
/// Gains {Mgc} ATK for each FIRE monster in your Banished pile.
/// </summary>
public sealed class Spirit_of_Flames : EffectMonsterCard
{
    private static readonly LocString BanishPrompt = new("cards", "YGODUELIST-SPIRIT_OF_FLAMES.banish_fire_graveyard");

    public Spirit_of_Flames()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 17,
            baseDef: 10,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fire | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Spirit_of_Flames) };

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanResolveHandSpecialSummon(Owner));

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();
        int n = CountFireMonstersInBanished(Owner);
        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        int bonus = System.Math.Max(0, n * mgc);
        return (bonus, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            Player? player = Owner;
            if (player == null || !CanResolveHandSpecialSummon(player))
                return;

            BaseMonsterCard? banish = await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(BanishPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildFireGraveyardCandidates(player));
            if (banish == null)
                return;

            await YgoBanishedService.BanishCard(player, banish);
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    private static int CountFireMonstersInBanished(Player player)
    {
        CardPile? banished = YgoPlayerPiles.Banished(player);
        if (banished == null)
            return 0;
        return YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(banished.Cards)
            .OfType<BaseMonsterCard>()
            .Count(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Fire);
    }

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return BuildFireGraveyardCandidates(player).Count >= 1
            && ReactorSlimeSummonGate.AllowsSummonPrintedRace(player, DuelMonsterRace.Pyro);
    }

    private static List<BaseMonsterCard> BuildFireGraveyardCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Fire && m is not Spirit_of_Flames)
            .ToList();

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
