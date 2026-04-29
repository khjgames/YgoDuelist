using System.Collections.Generic;
using System.Linq;
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

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 1 WIND from your Graveyard to Special Summon.
/// When destroyed by battle: draw {Mgc}, then you may destroy up to {Mgc} cards in your hand.
/// </summary>
public sealed class Silpheed : EffectMonsterCard
{
    private static readonly LocString BanishWindPrompt =
        new("cards", "YGODUELIST-SILPHEED.banish_wind_graveyard");

    private static readonly LocString OptionalHandDestroyPrompt =
        new("cards", "YGODUELIST-SILPHEED.optional_hand_destroy");

    public Silpheed()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 17,
            baseDef: 7,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Wind | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Silpheed) };

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanResolveHandSpecialSummon(Owner));

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
                new CardSelectorPrefs(BanishWindPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildWindGraveyardCandidates(player));
            if (banish == null)
                return;

            await YgoBanishedService.BanishCard(player, banish);
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    public override async Task OnPetDiedAfterOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        if (ctx.CommandState?.DestroyedByEnemyBattleDamage == true)
            await ResolveBattleDestructionAsync(ctx.Player);
        await base.OnPetDiedAfterOptionPileHandlingAsync(ctx);
    }

    private async Task ResolveBattleDestructionAsync(Player player)
    {
        if (player.Creature == null)
            return;

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        if (mgc <= 0)
            return;

        var choiceContext = YgoChoiceContexts.Blocking();
        await CardPileCmd.Draw(choiceContext, mgc, player);

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return;

        List<CardModel> candidates = TributeSummonGridSelect.BuildStabilizedHandCandidates(player, null, null);
        if (candidates.Count == 0)
            return;

        int maxPick = System.Math.Min(mgc, candidates.Count);
        List<CardModel> picked = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(OptionalHandDestroyPrompt, 0, maxPick)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => TributeSummonGridSelect.BuildStabilizedHandCandidates(player, null, null),
            maxResults: maxPick);
        if (picked.Count == 0)
            return;

        foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(picked))
        {
            await CardPileCmd.Add(new[] { c }, gy, CardPilePosition.Top, c, false);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return BuildWindGraveyardCandidates(player).Count >= 1;
    }

    private static List<BaseMonsterCard> BuildWindGraveyardCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Wind && m is not Silpheed)
            .ToList();
}
