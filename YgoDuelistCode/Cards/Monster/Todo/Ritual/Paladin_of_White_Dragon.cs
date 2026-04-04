using System;
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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Ritual;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Ritual;

public sealed class Paladin_of_White_Dragon : RitualMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString BlueEyesPickPrompt =
        new("cards", "YGODUELIST-PALADIN_OF_WHITE_DRAGON.activated_effect.selection");

    public Paladin_of_White_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 19,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Ritual | YgoCardPackTags.Light | YgoCardPackTags.Dragon;

    public override Type[] BundledCards => new[] { typeof(White_Dragon_Ritual), typeof(Paladin_of_White_Dragon) };

    public override Type[] RelatedCards =>
        RitualArchetypeMeta
            .RelatedCardsForPairedRitual(typeof(White_Dragon_Ritual), typeof(Paladin_of_White_Dragon))
            .Concat(new[] { typeof(Blue_Eyes_White_Dragon) })
            .ToArray();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;

            CardModel bewdPreview = YgoPackCardCatalog.CardFromType(typeof(Blue_Eyes_White_Dragon));
            yield return HoverTipFactory.FromCard(bewdPreview);
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-PALADIN_OF_WHITE_DRAGON.activated_effect.description";

    public bool IsActivatedEffectAvailable => IsActivatedEffectPlayable(Owner);

    public bool IsActivatedEffectPlayable(Player? player)
    {
        Player? p = player ?? Owner;
        if (p == null)
            return false;
        return DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(p, tributeReleaseCount: 1)
            && GetEligibleBlueEyesWhiteDragons(p).Count > 0;
    }

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        List<Blue_Eyes_White_Dragon> candidates = GetEligibleBlueEyesWhiteDragons(player);
        if (candidates.Count == 0)
            return false;

        if (candidates.Count == 1)
        {
            ActivatedEffectTributeSelectionPayload.SetPending(source, candidates[0]);
            return true;
        }

        var prefs = new CardSelectorPrefs(BlueEyesPickPrompt, 1, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };

        IEnumerable<CardModel> picked;
        try
        {
            picked = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates.Cast<CardModel>().ToList(),
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        Blue_Eyes_White_Dragon? chosen = picked.OfType<Blue_Eyes_White_Dragon>().FirstOrDefault();
        if (chosen == null)
            return false;

        ActivatedEffectTributeSelectionPayload.SetPending(source, chosen);
        return true;
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.PlayerCombatState == null)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out BaseMonsterCard? pending)
            || pending is not Blue_Eyes_White_Dragon bewd)
            return;

        if (!IsBlueEyesInHandDiscardOrDraw(player, bewd))
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);

        await CreatureCmd.Kill(pet, force: true);

        CardPile? grave = GraveyardPile.CustomType.GetPile(player);
        if (grave != null)
            await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, bewd, choiceContext))
            return;

        Creature? bewdPet = TributeSummonSelection.ResolvePetForFieldCard(player, bewd);
        if (bewdPet == null || !bewdPet.IsAlive || player.Creature == null)
            return;

        // Same rule as <see cref="DuelMonsterSummon.TrySummonDuelMonster"/> with canAttackThisTurn: false:
        // registry lock + Stiff/Fatigue so Command Attack (and Command Defend for normal monsters) cannot be used this turn.
        // Stumbling Field already blocks Command Attack on special summons (defend-only power from summon flow).
        if (!YgoStumblingField.IsActive(player))
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(bewdPet, true, player.Creature, bewd);
    }

    protected override async Task OnAfterMonsterAttackHitAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        AttackCommand attackCommand)
    {
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
        Creature? applier = pet ?? Owner?.Creature;
        if (applier == null)
            return;

        foreach (DamageResult r in attackCommand.Results)
        {
            if (r.Receiver.Side != CombatSide.Enemy || r.BlockedDamage <= 0 || !r.Receiver.IsAlive)
                continue;
            await PowerCmd.Apply<VulnerablePower>(r.Receiver, 1m, applier, this);
        }
    }

    private static List<Blue_Eyes_White_Dragon> GetEligibleBlueEyesWhiteDragons(Player player)
    {
        var list = new List<Blue_Eyes_White_Dragon>();
        AppendBlueEyesFromPile(PileType.Hand.GetPile(player), list);
        AppendBlueEyesFromPile(PileType.Discard.GetPile(player), list);
        AppendBlueEyesFromPile(PileType.Draw.GetPile(player), list);
        return list;
    }

    private static void AppendBlueEyesFromPile(CardPile? pile, List<Blue_Eyes_White_Dragon> list)
    {
        if (pile == null)
            return;
        foreach (CardModel c in pile.Cards)
        {
            if (c is Blue_Eyes_White_Dragon bewd)
                list.Add(bewd);
        }
    }

    private static bool IsBlueEyesInHandDiscardOrDraw(Player player, Blue_Eyes_White_Dragon bewd)
    {
        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand != null && hand.Cards.Contains(bewd))
            return true;
        CardPile? discard = PileType.Discard.GetPile(player);
        if (discard != null && discard.Cards.Contains(bewd))
            return true;
        CardPile? draw = PileType.Draw.GetPile(player);
        return draw != null && draw.Cards.Contains(bewd);
    }
}
