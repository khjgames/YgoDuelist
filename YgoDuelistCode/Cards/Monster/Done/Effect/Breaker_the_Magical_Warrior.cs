using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Breaker_the_Magical_Warrior : EffectMonsterCard, IMonsterActivatedEffect, IYgoSpellCounterMonster
{
    private static readonly LocString DestroyPrompt = new("cards", "YGODUELIST-BREAKER_THE_MAGICAL_WARRIOR.destroy_spell_trap");

    [SavedProperty]
    public int YgoDuelist_SpellCounters { get; set; }

    public Breaker_the_Magical_Warrior()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 16,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Breaker_the_Magical_Warrior) };

    public int CurrentSpellCounters => YgoDuelist_SpellCounters;
    public int MaxSpellCounters => 1;

    public void AddSpellCounter(int amount = 1)
    {
        if (amount <= 0)
            return;
        YgoDuelist_SpellCounters = Math.Min(MaxSpellCounters, YgoDuelist_SpellCounters + amount);
    }

    public bool TryConsumeSpellCounters(int amount)
    {
        if (amount <= 0)
            return true;
        if (YgoDuelist_SpellCounters < amount)
            return false;
        YgoDuelist_SpellCounters -= amount;
        return true;
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-BREAKER_THE_MAGICAL_WARRIOR.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner?.Creature?.CombatState != null
        && CurrentSpellCounters >= 1
        && BuildSpellTrapTargets(Owner.Creature.CombatState).Count > 0;

    protected override (int atk, int def) GetSecondaryStats() =>
        (CurrentSpellCounters * 3, 0);

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        _ = duelMonsterPet;
        await RunOnNormalOrTributeSummonAsync(
            player,
            choiceContext,
            duelMonsterPet,
            _ =>
            {
                AddSpellCounter(1);
                return Task.CompletedTask;
            });
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        _ = cardPlay;
        if (source is not Breaker_the_Magical_Warrior breaker || breaker.Owner?.Creature?.CombatState == null)
            return;

        Player player = breaker.Owner;
        List<CardModel> candidates = BuildSpellTrapTargets(player.Creature.CombatState);
        if (candidates.Count == 0)
            return;
        if (!TryConsumeSpellCounters(1))
            return;

        CardModel? target = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(DestroyPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildSpellTrapTargets(player.Creature.CombatState));
        if (target == null)
        {
            AddSpellCounter(1);
            return;
        }

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(breaker, player);
        if (pet == null)
            return;

        bool destroyed = await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(target, breaker);
        if (!destroyed)
        {
            AddSpellCounter(1);
            return;
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    private static List<CardModel> BuildSpellTrapTargets(MegaCrit.Sts2.Core.Combat.CombatState cs)
    {
        var spells = new List<BaseSpellCard>();
        var traps = new List<BaseTrapCard>();
        YgoFlipSpellTrapFieldEffects.CollectSpellsInAllSpellTrapZones(cs, spells);
        YgoFlipSpellTrapFieldEffects.CollectTrapsInAllSpellTrapZones(cs, traps);
        return YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(spells.Cast<CardModel>().Concat(traps))
            .ToList();
    }
}
