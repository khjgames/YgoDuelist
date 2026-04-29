using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// While face-up on the field: Warrior duel monsters you control gain Magic Protection (Spell/Trap destruction protection).
/// Upgraded: they also gain Monster Protection.
/// </summary>
public sealed class Frontier_Wiseman : EffectMonsterCard, IYgoOwnerTurnStartFieldMonsterEffect
{
    private static readonly CardKeyword MagicProtectionKeyword = (CardKeyword)20063;
    private static readonly CardKeyword MonsterProtectionKeyword = (CardKeyword)20064;

    public Frontier_Wiseman()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 16,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Earth | YgoCardPackTags.Spellcaster | YgoCardPackTags.Warrior;

    public override bool UseAlternateUpgradedDescription => true;

    public override Type[] RelatedCards => new[] { typeof(Frontier_Wiseman) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<MagicProtectionKeywordPower>();
            yield return HoverTipFactory.FromPower<MonsterProtectionKeywordPower>();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            foreach (CardKeyword kw in base.CanonicalKeywords)
                yield return kw;
            yield return MagicProtectionKeyword;
            if (IsUpgraded)
                yield return MonsterProtectionKeyword;
        }
    }

    public bool IsOwnerTurnStartFieldMonsterEffectActive() =>
        Owner != null && !FaceDown && DuelMonsterFieldRegistry.ContainsFieldMonster(Owner, this);

    public Task TryResolveOwnerTurnStartFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner) =>
        SyncWarriorShieldsAsync(choiceContext, owner, ResolveActiveFrontier(owner));

    public override void ScheduleFlipFaceUpSideEffectsBeforeFlipPipeline()
    {
        base.ScheduleFlipFaceUpSideEffectsBeforeFlipPipeline();
        Player? pl = Owner;
        if (pl?.PlayerCombatState != null)
            TaskHelper.RunSafely(SyncWarriorShieldsAsync(YgoChoiceContexts.Blocking(), pl, this));
    }

    /// <summary>After any duel summon: re-tag Warriors if a face-up Frontier is on the field.</summary>
    public static Task SyncBoardAfterAnyDuelSummonAsync(PlayerChoiceContext ctx, Player owner) =>
        SyncWarriorShieldsAsync(ctx, owner, ResolveActiveFrontier(owner));

    private static Frontier_Wiseman? ResolveActiveFrontier(Player? owner)
    {
        if (owner == null)
            return null;
        foreach (Frontier_Wiseman fw in DuelMonsterFieldRegistry.OrderedFieldMonstersOfType<Frontier_Wiseman>(owner))
        {
            if (!fw.FaceDown)
                return fw;
        }

        return null;
    }

    public override async Task OnAfterSummonPipelineAsync(
        Player player,
        PlayerChoiceContext ctx,
        Creature pet,
        bool canAttackThisTurn)
    {
        await base.OnAfterSummonPipelineAsync(player, ctx, pet, canAttackThisTurn);
        if (!FaceDown && DuelMonsterFieldRegistry.HasSourceCard(pet, this))
            await SyncWarriorShieldsAsync(ctx, player, this);
    }

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        base.OnMovedToGraveyardFromHandOrField(from);
        Player? pl = Owner;
        if (pl?.PlayerCombatState != null)
            TaskHelper.RunSafely(SyncWarriorShieldsAsync(YgoChoiceContexts.Blocking(), pl, null));
    }

    internal static async Task SyncWarriorShieldsAsync(
        PlayerChoiceContext choiceContext,
        Player? owner,
        Frontier_Wiseman? frontier)
    {
        _ = choiceContext;
        if (owner?.PlayerCombatState == null || owner.Creature == null)
            return;

        await StripAllFrontierShieldsAsync(owner);

        if (frontier == null
            || frontier.FaceDown
            || !DuelMonsterFieldRegistry.ContainsFieldMonster(owner, frontier))
            return;

        Creature? frontierPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(frontier, owner);
        if (frontierPet == null || !frontierPet.IsAlive)
            return;

        bool monsterToo = frontier.IsUpgraded;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard bm)
                continue;
            if (bm.DuelMonsterRace != DuelMonsterRace.Warrior)
                continue;

            await PowerCmd.Apply<FrontierWisemanSpellShieldPower>(pet, 1m, owner.Creature, frontier);
            if (monsterToo)
                await PowerCmd.Apply<FrontierWisemanMonsterShieldPower>(pet, 1m, owner.Creature, frontier);
        }
    }

    private static async Task StripAllFrontierShieldsAsync(Player owner)
    {
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(owner.PlayerCombatState))
        {
            if (pet.GetPower<FrontierWisemanSpellShieldPower>() != null)
                await PowerCmd.Remove<FrontierWisemanSpellShieldPower>(pet);
            if (pet.GetPower<FrontierWisemanMonsterShieldPower>() != null)
                await PowerCmd.Remove<FrontierWisemanMonsterShieldPower>(pet);
        }
    }
}
