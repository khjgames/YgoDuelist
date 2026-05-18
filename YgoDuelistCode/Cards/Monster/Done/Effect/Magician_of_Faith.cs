using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>FLIP: Target 1 Spell in your Graveyard; add that target to your hand.</summary>
public sealed class Magician_of_Faith : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString SpellPrompt = new("cards", "YGODUELIST-MAGICIAN_OF_FAITH.flip_select_spell");

    public Magician_of_Faith()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 3,
            baseDef: 4,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[]
    {
        typeof(Magician_of_Faith),
        typeof(Graceful_Charity),
    };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Magician_of_Faith)
            return;

        Player? player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        List<BaseSpellCard> candidates = BuildSpellCandidates(player);
        if (candidates.Count == 0)
            return;

        BaseSpellCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(SpellPrompt, 1, 1) { Cancelable = true },
            () => BuildSpellCandidates(player));
        if (chosen == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, hand, CardPilePosition.Top, chosen, false);
    }

    private static List<BaseSpellCard> BuildSpellCandidates(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseSpellCard>()
            .ToList();
}
