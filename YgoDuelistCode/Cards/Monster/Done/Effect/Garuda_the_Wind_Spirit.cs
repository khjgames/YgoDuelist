using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Hand: banish 1 WIND from your Graveyard to Special Summon.
/// Activate: change 1 face-up monster's battle position (no Stiff from this change; works even if that monster has Stiff).
/// </summary>
public sealed class Garuda_the_Wind_Spirit : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString BanishPrompt =
        new("cards", "YGODUELIST-GARUDA_THE_WIND_SPIRIT.banish_selection");

    private static readonly LocString ChangePositionPrompt =
        new("cards", "YGODUELIST-GARUDA_THE_WIND_SPIRIT.change_battle_position");

    public Garuda_the_Wind_Spirit()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 16,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Wind | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Garuda_the_Wind_Spirit) };

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive;

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && IsHandEffectFormActive && CanResolveHandSpecialSummon(Owner);

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey =>
        "YGODUELIST-GARUDA_THE_WIND_SPIRIT.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            if (pet == null || !MonsterCommandRegistry.TryGet(pet, out var cmd) || cmd.HasUsedActivatedEffectThisTurn)
                return false;
            return BuildFaceUpMonsterPositionTargets(Owner.Creature.CombatState).Count > 0;
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Garuda_the_Wind_Spirit || Owner?.Creature?.CombatState == null)
            return;

        Player player = Owner;
        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || player.Creature == null)
            return;

        CombatState cs = player.Creature.CombatState;
        List<NormalMonsterCard> candidates = BuildFaceUpMonsterPositionTargets(cs);
        if (candidates.Count == 0)
            return;

        NormalMonsterCard? target = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(ChangePositionPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildFaceUpMonsterPositionTargets(cs));
        if (target?.Owner == null)
            return;

        Player fieldOwner = target.Owner;
        if (fieldOwner.PlayerCombatState == null || fieldOwner.Creature == null)
            return;

        Creature? targetPet = YgoMpCombatOrder.FirstPetWhere(
            fieldOwner.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, target));
        if (targetPet == null)
            return;

        bool nextAttack = !target.IsAttackBattlePosition;
        target.SetBattlePositionFromDuelCommand(nextAttack);
        await DuelMonsterStancePowerSync.SyncForPetAsync(targetPet, target, fieldOwner.Creature, target);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
    }

    private static List<NormalMonsterCard> BuildFaceUpMonsterPositionTargets(CombatState cs)
    {
        var list = new List<NormalMonsterCard>();
        foreach (Player p in YgoMpCombatOrder.PlayersSnapshotOrderedByNetId(cs.Players))
        {
            foreach (BaseMonsterCard m in DuelMonsterFieldRegistry.OrderedFieldMonsters(p))
            {
                if (m.FaceDown || m is not NormalMonsterCard nm)
                    continue;
                list.Add(nm);
            }
        }

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(list).OfType<NormalMonsterCard>().ToList();
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
                () => BuildWindGraveyardCandidates(player));
            if (banish == null)
                return;

            await YgoBanishedService.BanishCard(player, banish);
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
        return BuildWindGraveyardCandidates(player).Count >= 1
            && ReactorSlimeSummonGate.AllowsSummonPrintedRace(player, DuelMonsterRace.WingedBeast);
    }

    private static List<BaseMonsterCard> BuildWindGraveyardCandidates(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Wind && m is not Garuda_the_Wind_Spirit)
            .ToList();
}
