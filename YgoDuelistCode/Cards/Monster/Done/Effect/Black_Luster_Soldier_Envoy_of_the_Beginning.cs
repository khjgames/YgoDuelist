using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Ritual;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 1 LIGHT and 1 DARK from your Graveyard to Special Summon.
/// Activate: banish 1 other monster you control; inflict 1 Vulnerable on target enemy.
/// On execute kill: remove Fatigue from this monster; gain Fleeting Followup (next Command Attack costs 0; expires end of turn if unused).
/// </summary>
public sealed class Black_Luster_Soldier_Envoy_of_the_Beginning : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString BanishLightPrompt =
        new("cards", "YGODUELIST-BLACK_LUSTER_SOLDIER_ENVOY_OF_THE_BEGINNING.banish_light_graveyard");

    private static readonly LocString BanishDarkPrompt =
        new("cards", "YGODUELIST-BLACK_LUSTER_SOLDIER_ENVOY_OF_THE_BEGINNING.banish_dark_graveyard");

    private static readonly LocString BanishOtherControlledMonsterPrompt =
        new("cards", "YGODUELIST-BLACK_LUSTER_SOLDIER_ENVOY_OF_THE_BEGINNING.banish_other_controlled_monster");

    public Black_Luster_Soldier_Envoy_of_the_Beginning()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 30,
            baseDef: 25,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Dark | YgoCardPackTags.Warrior | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Black_Luster_Soldier_Envoy_of_the_Beginning), typeof(Black_Luster_Soldier) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<VulnerablePower>();
            yield return HoverTipFactory.FromPower<FleetingFollowupPower>();
        }
    }

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

            BaseMonsterCard? light = await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(BanishLightPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildGraveyardAttributeCandidates(player, DuelMonsterAttribute.Light));
            if (light == null)
                return;

            BaseMonsterCard? dark = await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(BanishDarkPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true,
                },
                () => BuildGraveyardAttributeCandidates(player, DuelMonsterAttribute.Dark, exclude: light));
            if (dark == null)
                return;

            await YgoBanishedService.BanishCard(player, light);
            await YgoBanishedService.BanishCard(player, dark);
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey =>
        "YGODUELIST-BLACK_LUSTER_SOLDIER_ENVOY_OF_THE_BEGINNING.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            if (pet == null || !MonsterCommandRegistry.TryGet(pet, out var cmd) || cmd.HasUsedActivatedEffectThisTurn)
                return false;
            if (BuildOtherControlledFaceUpMonsterCandidates(Owner, excludeSource: this).Count == 0)
                return false;
            return Owner.Creature.CombatState.HittableEnemies.Any(e => e.IsAlive);
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Black_Luster_Soldier_Envoy_of_the_Beginning || Owner?.Creature?.CombatState == null)
            return;

        Creature? targetEnemy = cardPlay.Target;
        if (targetEnemy == null || !targetEnemy.IsAlive || targetEnemy.Side != CombatSide.Enemy)
            return;

        Player player = Owner;
        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || player.Creature == null)
            return;

        List<(Creature pet, BaseMonsterCard card)> candidates = BuildOtherControlledFaceUpMonsterCandidates(player, excludeSource: this);
        if (candidates.Count == 0)
            return;

        BaseMonsterCard? pick = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(BanishOtherControlledMonsterPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => candidates.Select(t => t.card).ToList());
        if (pick == null)
            return;

        (Creature pet, BaseMonsterCard card)? chosen = candidates.FirstOrDefault(t => ReferenceEquals(t.card, pick));
        if (chosen == null)
            return;

        await DuelMonsterPetDeathPatch.ReleaseLiveFieldMonsterToBanishedAsync(
            player,
            chosen.Value.pet,
            chosen.Value.card);

        await PowerCmd.Apply<VulnerablePower>(targetEnemy, 1m, player.Creature, source);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
    }

    public override async Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs)
    {
        _ = cs;
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;
        bool anyKill = command.Results.Any(r => r.Receiver.Side == CombatSide.Enemy && r.WasTargetKilled);
        if (!anyKill)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this);
        if (pet == null || !pet.IsAlive)
            return;

        if (pet.GetPower<FatiguePower>() != null)
            await PowerCmd.Remove<FatiguePower>(pet);
        await PowerCmd.Apply<FleetingFollowupPower>(pet, 1m, Owner.Creature, this);
    }

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return BuildGraveyardAttributeCandidates(player, DuelMonsterAttribute.Light).Count >= 1
            && BuildGraveyardAttributeCandidates(player, DuelMonsterAttribute.Dark).Count >= 1;
    }

    private static List<BaseMonsterCard> BuildGraveyardAttributeCandidates(
        Player player,
        DuelMonsterAttribute attribute,
        BaseMonsterCard? exclude = null) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterAttribute == attribute && (exclude == null || !ReferenceEquals(m, exclude)))
            .ToList();

    private static List<(Creature pet, BaseMonsterCard card)> BuildOtherControlledFaceUpMonsterCandidates(
        Player actingPlayer,
        BaseMonsterCard excludeSource)
    {
        var list = new List<(Creature, BaseMonsterCard)>();
        if (actingPlayer.PlayerCombatState == null)
            return list;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(actingPlayer.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            BaseMonsterCard? m = DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet);
            if (m == null || m.FaceDown || ReferenceEquals(m, excludeSource))
                continue;
            list.Add((pet, m));
        }

        return list;
    }
}
