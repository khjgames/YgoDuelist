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
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Apprentice_Magician : EffectMonsterCard, IYgoSpellCounterMonster, IBattleDeathOptionalDeckSpecialSummon
{
    private static readonly LocString SelectPrompt = new("cards", "YGODUELIST-APPRENTICE_MAGICIAN.select_spell_counter_target");
    private static readonly LocString BattleDeathActivatePromptLoc =
        new("cards", "YGODUELIST-APPRENTICE_MAGICIAN.activate_destroyed_by_battle");
    private static readonly LocString BattleDeathSummonPromptLoc =
        new("cards", "YGODUELIST-APPRENTICE_MAGICIAN.summon_spellcaster_fd");

    [SavedProperty]
    public int SpellCounters { get; set; }

    public Apprentice_Magician()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 4,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Spellcaster | YgoCardPackTags.Dark | YgoCardPackTags.Spell;

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathActivatePrompt => BattleDeathActivatePromptLoc;

    LocString IBattleDeathOptionalDeckSpecialSummon.BattleDeathSummonPrompt => BattleDeathSummonPromptLoc;

    bool IBattleDeathOptionalDeckSpecialSummon.IsBattleDeathDeckSummonCandidate(BaseMonsterCard m) =>
        m.DuelMonsterRace == DuelMonsterRace.Spellcaster
        && m.DuelMonsterLevel <= 2
        && m.CanSummonDuelMonster;

    public int CurrentSpellCounters => SpellCounters;

    public int MaxSpellCounters => 1;

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

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnNormalOrTributeSummonAsync(
            player,
            choiceContext,
            duelMonsterPet,
            ctx => TryPlaceSpellCounterOnFieldAsync(ctx, player));

    public override async Task OnFlipSummonedFromCommandMenuAsync(PlayerChoiceContext choiceContext, Player player) =>
        await RunOnFlipSummonedFromCommandMenuAsync(
            choiceContext,
            ctx => TryPlaceSpellCounterOnFieldAsync(ctx, player));

    private static async Task TryPlaceSpellCounterOnFieldAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.PlayerCombatState == null)
            return;

        List<BaseMonsterCard> candidates = BuildSpellCounterTargets(player);
        if (candidates.Count == 0)
            return;

        BaseMonsterCard? target = candidates.Count == 1
            ? candidates[0]
            : await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                new CardSelectorPrefs(SelectPrompt, 1, 1) { Cancelable = true },
                () => BuildSpellCounterTargets(player));
        if (target is not IYgoSpellCounterMonster)
            return;

        if (target is IYgoSpellCounterMonster m)
            m.AddSpellCounter(1);
    }

    private static List<BaseMonsterCard> BuildSpellCounterTargets(Player player)
    {
        var candidates = new List<BaseMonsterCard>();
        foreach (BaseMonsterCard field in DuelMonsterFieldRegistry.OrderedFieldMonsters(player))
        {
            if (field is not IYgoSpellCounterMonster counterMonster)
                continue;
            if (field is not AbstractMonsterCard am || am.FaceDown)
                continue;
            if (counterMonster.CurrentSpellCounters >= counterMonster.MaxSpellCounters)
                continue;
            candidates.Add(field);
        }

        return candidates;
    }
}
