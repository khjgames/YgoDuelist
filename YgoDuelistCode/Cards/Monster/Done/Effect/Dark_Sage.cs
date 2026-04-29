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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Normal Summoned/Set. Special Summon only via <see cref="YgoDuelist.YgoDuelistCode.Cards.Command.Special_Summon_Dark_Sage"/>
/// on <see cref="Dark_Magician"/> with <see cref="Dark_Magician.SurvivedTimeMagic"/>. On summon: add 1 Spell from your Deck to your hand.
/// </summary>
public sealed class Dark_Sage : EffectMonsterCard
{
    private static readonly LocString AddSpellPrompt = new("cards", "YGODUELIST-DARK_SAGE.add_spell_from_deck");

    public Dark_Sage()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 9,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 28,
            baseDef: 32,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Spell;

    public override Type[] RelatedCards =>
        new[] { typeof(Dark_Sage), typeof(Dark_Magician), typeof(Time_Wizard) };

    public override bool CanSummonDuelMonster => false;

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate =>
        YgoDarkSageSummonGate.IsSummonBypassActive;

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet)
    {
        await base.OnSummoned(player, choiceContext, duelMonsterPet);

        List<BaseSpellCard> candidates = BuildDeckSpellCandidates(player);
        if (candidates.Count == 0)
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        BaseSpellCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(AddSpellPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => BuildDeckSpellCandidates(player));
        if (chosen == null)
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null || chosen.Pile != draw)
            return;

        await CardPileCmd.Add(new[] { chosen }, hand, CardPilePosition.Top, chosen, false);
    }

    private static List<BaseSpellCard> BuildDeckSpellCandidates(Player player)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return new List<BaseSpellCard>();

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards).OfType<BaseSpellCard>().ToList();
    }
}
