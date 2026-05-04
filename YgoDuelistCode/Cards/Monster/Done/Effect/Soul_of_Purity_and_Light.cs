using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 2 LIGHT from your Graveyard to Special Summon.
/// Each of your turn starts while face-up: inflict {Mgc} Temporary Strength Loss on all enemies (<see cref="TryResolveOwnerTurnStartFieldMonsterEffectAsync"/>).
/// The turn it is Special Summoned is covered by <see cref="OnAfterSummonPipelineAsync"/> (same pulse helper); <see cref="TryResolveOwnerTurnStartFieldMonsterEffectAsync"/>
/// skips if that pet already pulsed this owner-turn stamp (<see cref="YgoSoulOfPurityAndLightTurnPulseDedup"/>) so nested summons during turn-start cannot double-apply.
/// </summary>
public sealed class Soul_of_Purity_and_Light : EffectMonsterCard, IYgoOwnerTurnStartFieldMonsterEffect
{
    private static readonly LocString BanishPrompt =
        new("cards", "YGODUELIST-SOUL_OF_PURITY_AND_LIGHT.banish_two_light");

    public Soul_of_Purity_and_Light()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 20,
            baseDef: 18,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Light | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Soul_of_Purity_and_Light) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<YgoTemporaryStrengthLossPower>();
        }
    }

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanResolveHandSpecialSummon(Owner));

    public bool IsOwnerTurnStartFieldMonsterEffectActive() =>
        Owner != null && !FaceDown && DuelMonsterFieldRegistry.ContainsFieldMonster(Owner, this);

    public async Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        _ = choiceContext;
        if (owner.PlayerCombatState == null || owner.Creature == null)
            return;
        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            owner.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, this));
        if (pet == null || YgoSoulOfPurityAndLightTurnPulseDedup.AlreadyPulsedThisOwnerTurn(owner, pet))
            return;
        await PulseTemporaryStrengthLossOnAllEnemiesAsync(owner);
        YgoSoulOfPurityAndLightTurnPulseDedup.NotePulse(owner, pet);
    }

    public override async Task OnAfterSummonPipelineAsync(
        Player player,
        PlayerChoiceContext ctx,
        Creature pet,
        bool canAttackThisTurn)
    {
        await base.OnAfterSummonPipelineAsync(player, ctx, pet, canAttackThisTurn);
        if (player.Creature?.CombatState == null)
            return;
        if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
            return;
        if (FaceDown)
            return;
        if (YgoSoulOfPurityAndLightTurnPulseDedup.AlreadyPulsedThisOwnerTurn(player, pet))
            return;
        await PulseTemporaryStrengthLossOnAllEnemiesAsync(player);
        YgoSoulOfPurityAndLightTurnPulseDedup.NotePulse(player, pet);
    }

    private async Task PulseTemporaryStrengthLossOnAllEnemiesAsync(Player owner)
    {
        if (owner.Creature?.CombatState is not { } cs)
            return;
        decimal loss = DynamicVars["Mgc"].BaseValue;
        if (loss <= 0m)
            return;
        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
            await PowerCmd.Apply<YgoTemporaryStrengthLossPower>(enemy, loss, owner.Creature, this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            Player? player = Owner;
            if (player == null || !CanResolveHandSpecialSummon(player))
                return;

            List<BaseMonsterCard> banished = await YgoOrderedCardSelection.TryChooseManyAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(BanishPrompt, 2, 2) { RequireManualConfirmation = true, Cancelable = true },
                () => BuildLightGraveyardCandidates(player),
                maxResults: 2);
            if (banished.Count < 2)
                return;

            foreach (BaseMonsterCard m in banished)
                await YgoBanishedService.BanishCard(player, m);

            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return BuildLightGraveyardCandidates(player).Count >= 2;
    }

    private static List<BaseMonsterCard> BuildLightGraveyardCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Light)
            .ToList();

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
