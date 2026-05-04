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
/// Hand effect: while you have less than 25% HP, you can Special Summon this from your hand.
/// ATK is equal to total Blight on enemies. Activate: Tribute 1 field monster; inflict Blight on 1 enemy equal to that monster's ATK.
/// </summary>
public sealed class Endless_Decay : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Endless_Decay()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 0,
            baseDef: 0,
            baseMgc: 0,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Zombie | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Endless_Decay) };

    protected override TargetType NonAttackPlayTargetType => TargetType.AnyEnemy;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    protected override bool SupportsHandEffectForm => true;

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => IsHandEffectFormActive && IsPlayerUnderQuarterHp(Owner);

    public override int CurrentStarCost => IsHandEffectFormActive ? 0 : base.CurrentStarCost;

    protected override int MonsterConduitStarCost => IsHandEffectFormActive ? 0 : base.MonsterConduitStarCost;

    protected override bool IsPlayable =>
        base.IsPlayable && (!IsHandEffectFormActive || CanResolveHandSpecialSummon(Owner));

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner?.Creature?.CombatState == null)
            return base.GetSecondaryStats();

        int blight = 0;
        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState))
        {
            if (enemy.GetPower<BlightPower>() is { } p)
                blight += (int)p.Amount;
        }

        blight = Math.Clamp(blight, 0, 9999);
        return (blight, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsHandEffectFormActive)
        {
            Player? player = Owner;
            if (player == null || !CanResolveHandSpecialSummon(player))
                return;

            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, choiceContext);
            return;
        }

        await base.OnPlay(choiceContext, cardPlay);
    }

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-ENDLESS_DECAY.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && Owner.Creature?.CombatState != null
        && !FaceDown
        && BuildOtherFieldTributeCandidates(Owner, this).Count > 0
        && YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState).Count > 0
        && MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner) != null;

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState == null)
            return false;

        List<BaseMonsterCard> candidates = BuildOtherFieldTributeCandidates(player, source);
        if (candidates.Count == 0)
            return false;

        return await YgoActivatedEffectTributeSelection.TryPrepareSingleTributeAsync(
            player,
            source,
            candidates.Cast<CardModel>().ToList(),
            TributePrompt);
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.PlayerCombatState == null || source is not Endless_Decay)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null || cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out BaseMonsterCard? tribute) || tribute == null)
            return;

        Creature? tributePet = YgoMpCombatOrder.FirstPetWhere(
            player.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, tribute));
        if (tributePet == null || !tributePet.IsAlive)
            return;

        IReadOnlyList<BaseMonsterCard> field = DuelMonsterFieldRegistry.OrderedFieldMonsters(player);
        int blightStacks = (int)tribute.CalcDuelMonsterStats(field).Atk;
        if (blightStacks <= 0)
            return;

        await CreatureCmd.Kill(tributePet, force: true);
        await PowerCmd.Apply<BlightPower>(cardPlay.Target, blightStacks, selfPet, source);
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
    }

    private static bool IsPlayerUnderQuarterHp(Player? player)
    {
        Creature? hero = player?.Creature;
        if (hero == null || hero.MaxHp <= 0)
            return false;
        return hero.CurrentHp < hero.MaxHp * 0.25m;
    }

    private static bool CanResolveHandSpecialSummon(Player? player)
    {
        if (player?.PlayerCombatState == null)
            return false;
        if (!IsPlayerUnderQuarterHp(player))
            return false;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return false;
        return true;
    }

    private static List<BaseMonsterCard> BuildOtherFieldTributeCandidates(Player player, NormalMonsterCard source)
    {
        var list = new List<BaseMonsterCard>();
        if (player.PlayerCombatState == null)
            return list;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard c)
                continue;
            if (ReferenceEquals(c, source))
                continue;
            list.Add(c);
        }

        return list;
    }
}
