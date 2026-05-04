using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Hannibal_Necromancer : EffectMonsterCard, IMonsterActivatedEffect, IYgoSpellCounterMonster
{
    private static readonly LocString SelectPrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    [SavedProperty]
    public int SpellCounters { get; set; }

    public Hannibal_Necromancer()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 14,
            baseDef: 18,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spellcaster | YgoCardPackTags.Dark | YgoCardPackTags.Spell;
    public override Type[] RelatedCards => new[] { typeof(Hannibal_Necromancer) };

    public int CurrentSpellCounters => SpellCounters;
    public int MaxSpellCounters => 3;

    public void AddSpellCounter(int amount = 1)
    {
        if (amount <= 0)
            return;
        SpellCounters = Math.Min(MaxSpellCounters, SpellCounters + amount);
    }

    public bool TryConsumeSpellCounters(int amount)
    {
        if (amount <= 0)
            return true;
        if (SpellCounters < amount)
            return false;
        SpellCounters -= amount;
        return true;
    }

    protected internal override Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            () =>
            {
                AddSpellCounter(1);
                return Task.CompletedTask;
            });

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-HANNIBAL_NECROMANCER.activated_effect.description";
    public bool IsActivatedEffectAvailable => Owner != null && CurrentSpellCounters >= 3 && BuildSummonTargets(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player == null || pet == null || !TryConsumeSpellCounters(3))
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        if (BuildSummonTargets(player).Count == 0)
            return;

        List<BaseMonsterCard> targets = BuildSummonTargets(player);
        BaseMonsterCard? summon = targets.Count == 1
            ? targets[0]
            : await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(SelectPrompt, 1, 1) { Cancelable = true },
                () => BuildSummonTargets(player));
        if (summon == null)
            return;

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, summon, choiceContext))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    private static List<BaseMonsterCard> BuildSummonTargets(Player player)
    {
        var list = new List<BaseMonsterCard>();
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy == null)
            return list;

        foreach (CardModel c in YgoMpCombatOrder.CardsSnapshotOrderedForMp(gy.Cards))
        {
            if (c is not BaseMonsterCard bm)
                continue;
            if (bm.DuelMonsterRace != DuelMonsterRace.Fiend)
                continue;
            if (bm.BaseAtk > 15)
                continue;
            list.Add(bm);
        }

        return list;
    }
}
