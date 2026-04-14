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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Apprentice_Magician : EffectMonsterCard, IYgoSpellCounterMonster
{
    private static readonly LocString SelectPrompt = new("cards", "YGODUELIST-APPRENTICE_MAGICIAN.select_spell_counter_target");

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
        YgoCardPackTags.Starter | YgoCardPackTags.Spellcaster | YgoCardPackTags.Dark | YgoCardPackTags.Spell;

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

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);
        if (YgoDuelMonsterSummonStyleContext.CurrentNormalOrTribute != true)
            return;

        var ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        await TryPlaceSpellCounterOnFieldAsync(ctx, player);
    }

    public async Task OnFlipSummonedAsync(PlayerChoiceContext choiceContext, Player player)
    {
        var ctx = choiceContext ?? new BlockingPlayerChoiceContext();
        await TryPlaceSpellCounterOnFieldAsync(ctx, player);
    }

    private static async Task TryPlaceSpellCounterOnFieldAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.PlayerCombatState == null)
            return;

        var candidates = new List<BaseMonsterCard>();
        foreach (BaseMonsterCard field in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (field is not IYgoSpellCounterMonster counterMonster)
                continue;
            if (field is not AbstractMonsterCard am || am.FaceDown)
                continue;
            if (counterMonster.CurrentSpellCounters >= counterMonster.MaxSpellCounters)
                continue;
            candidates.Add(field);
        }

        if (candidates.Count == 0)
            return;

        BaseMonsterCard target = candidates[0];
        if (candidates.Count > 1)
        {
            IEnumerable<CardModel> pick;
            try
            {
                pick = await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    candidates.Cast<CardModel>().ToList(),
                    player,
                    new CardSelectorPrefs(SelectPrompt, 1, 1) { Cancelable = true });
            }
            catch (OperationCanceledException)
            {
                return;
            }

            BaseMonsterCard? chosen = pick.OfType<BaseMonsterCard>().FirstOrDefault();
            if (chosen is not IYgoSpellCounterMonster)
                return;
            target = chosen;
        }

        if (target is IYgoSpellCounterMonster m)
            m.AddSpellCounter(1);
    }
}
