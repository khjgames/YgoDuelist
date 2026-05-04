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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>On Normal or Special Summon: add 1 Spell from your Graveyard to your discard pile (Effect_Monsters_TODO).</summary>
public sealed class Dark_Magician_of_Chaos : EffectMonsterCard
{
    private static readonly LocString PickSpellPrompt = new("cards", "YGODUELIST-DARK_MAGICIAN_OF_CHAOS.summon_pick_spell");

    public Dark_Magician_of_Chaos()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 28,
            baseDef: 26,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Spell;

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.DarkMagician;

    public override Type[] RelatedCards => new[] { typeof(Dark_Magician_of_Chaos), typeof(Dark_Magician) };

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            async () =>
            {
                BaseSpellCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
                    EnsureBlockingChoiceContext(choiceContext),
                    player,
                    new CardSelectorPrefs(PickSpellPrompt, 1, 1) { Cancelable = true },
                    () => BuildSpellTargets(player));
                if (chosen == null)
                    return;

                CardPile? discard = YgoPlayerPiles.Discard(player);
                if (discard == null)
                    return;

                await CardPileCmd.Add(new[] { chosen }, discard, CardPilePosition.Top, chosen, false);
            });

    private static List<BaseSpellCard> BuildSpellTargets(Player player) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
        .OfType<BaseSpellCard>()
        .ToList();
}
