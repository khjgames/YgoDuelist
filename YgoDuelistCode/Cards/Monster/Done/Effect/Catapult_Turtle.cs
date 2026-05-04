using System;
using System.Collections.Generic;
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

public sealed class Catapult_Turtle : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Catapult_Turtle()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 10,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Water | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Catapult_Turtle) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-CATAPULT_TURTLE.activated_effect.description";

    public bool ActivatedEffectConsumesOncePerTurnSlot => false;

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.PlayerCombatState?.Pets == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            if (pet == null || !MonsterCommandRegistry.TryGet(pet, out var cmd) || cmd.CatapultTurtleActivatedThisTurn)
                return false;
            return YgoMpCombatOrder.PetsAny(
                Owner.PlayerCombatState,
                p =>
                    p.IsAlive
                    && DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(p) is BaseMonsterCard c
                    && c is not Catapult_Turtle);
        }
    }

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState?.Pets == null)
            return false;

        var candidates = new List<BaseMonsterCard>();
        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard c)
                continue;
            if (c is Catapult_Turtle)
                continue;
            candidates.Add(c);
        }

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
        if (player?.PlayerCombatState?.Pets == null)
            return;

        if (cardPlay.Target == null || !cardPlay.Target.IsAlive)
            return;

        Creature? sourcePet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (sourcePet == null)
            return;

        if (!MonsterCommandRegistry.TryGet(sourcePet, out var cmd) || cmd.CatapultTurtleActivatedThisTurn)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out var chosen) || chosen == null)
            return;

        Creature? tributePet = YgoMpCombatOrder.FirstPetWhere(
            player.PlayerCombatState,
            p => p.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(p, chosen));
        if (tributePet == null || !tributePet.IsAlive)
            return;

        IReadOnlyCollection<BaseMonsterCard> field = DuelMonsterFieldRegistry.OrderedFieldMonsters(player);
        int halfAtk = (int)Math.Floor(chosen.CalcDuelMonsterStats(field).Atk / 2m);
        if (halfAtk < 1)
            halfAtk = 1;

        await CreatureCmd.Kill(tributePet, force: true);

        CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
        if (graveyard != null)
            await CardPileCmd.Add(new[] { chosen }, graveyard, CardPilePosition.Top, chosen, false);

        cmd.CatapultTurtleActivatedThisTurn = true;
        await PowerCmd.Apply<BlightPower>(cardPlay.Target, halfAtk, sourcePet, source);
    }
}
