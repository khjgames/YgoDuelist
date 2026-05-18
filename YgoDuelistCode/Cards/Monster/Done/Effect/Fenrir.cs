using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 2 WATER from your Graveyard to Special Summon.
/// On execute kill: gain {Mgc} stacks of <see cref="YgoDeferredDrawNextTurnPower"/> (draw that many extra cards at the start of your next turn).
/// </summary>
public sealed class Fenrir : EffectMonsterCard
{
    private static readonly LocString BanishWaterPrompt =
        new("cards", "YGODUELIST-FENRIR.banish_two_water_graveyard");

    public Fenrir()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 14,
            baseDef: 12,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Water | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Fenrir) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<YgoDeferredDrawNextTurnPower>();
        }
    }

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && IsHandEffectFormActive && CanResolveHandSpecialSummon(Owner);

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
                new CardSelectorPrefs(BanishWaterPrompt, 2, 2) { RequireManualConfirmation = true, Cancelable = true },
                () => BuildWaterGraveyardCandidates(player),
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

    public override async Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs)
    {
        _ = cs;
        if (Owner?.Creature == null)
            return;
        bool anyKill = command.Results.Any(r => r.Receiver.Side == CombatSide.Enemy && r.WasTargetKilled);
        if (!anyKill)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this);
        if (pet == null || !pet.IsAlive || !DuelMonsterFieldRegistry.HasSourceCard(pet, this))
            return;

        decimal mgc = DynamicVars["Mgc"].BaseValue;
        if (mgc <= 0m)
            return;

        Creature hero = Owner.Creature;
        if (hero.GetPower<YgoDeferredDrawNextTurnPower>() is { } existing)
            await PowerCmd.ModifyAmount(existing, mgc, hero, this);
        else
            await PowerCmd.Apply<YgoDeferredDrawNextTurnPower>(hero, mgc, hero, this);
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
        return BuildWaterGraveyardCandidates(player).Count >= 2
            && ReactorSlimeSummonGate.AllowsSummonPrintedRace(player, DuelMonsterRace.Beast);
    }

    private static List<BaseMonsterCard> BuildWaterGraveyardCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Water && m is not Fenrir)
            .ToList();
}
