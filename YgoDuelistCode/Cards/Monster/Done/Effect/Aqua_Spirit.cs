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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Cannot be Normal Summoned. Hand Effect: Banish 1 WATER monster from your Graveyard; Special Summon this card.</summary>
public sealed class Aqua_Spirit : EffectMonsterCard
{
    private static readonly LocString BanishPrompt =
        new("cards", "YGODUELIST-AQUA_SPIRIT.banish_selection");

    public Aqua_Spirit()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 16,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Water | YgoCardPackTags.Ocean | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Aqua_Spirit) };

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanResolveHandSpecialSummon(Owner));

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return BuildWaterGraveyardCandidates(player).Count >= 1;
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
                () => BuildWaterGraveyardCandidates(player));
            if (banish == null)
                return;

            await YgoBanishedService.BanishCard(player, banish);
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    private static List<BaseMonsterCard> BuildWaterGraveyardCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Water && m is not Aqua_Spirit)
            .ToList();
}
