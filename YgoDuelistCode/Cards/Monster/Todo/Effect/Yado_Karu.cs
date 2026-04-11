using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Yado_Karu : EffectMonsterCard
{
    private static readonly LocString HandToDeckBottomPrompt =
        new LocString("cards", "YGODUELIST-YADO_KARU.position_change.hand_select");

    public Yado_Karu()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 9,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Yado_Karu),
    };

    public override Task OnSwitchedFromAttackToDefenseFromCommandAsync(PlayerChoiceContext choiceContext, Player player) =>
        ResolveAttackToDefenseEffectAsync(choiceContext, player);

    private async Task ResolveAttackToDefenseEffectAsync(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature?.CombatState == null)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null || hand.Cards.Count == 0)
            return;

        List<CardModel> candidates = TributeSummonGridSelect.BuildStabilizedHandCandidates(player, null, null);
        int maxSelectable = candidates.Count;
        var prefs = new CardSelectorPrefs(HandToDeckBottomPrompt, 0, maxSelectable)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        IEnumerable<CardModel> picked = await TributeSummonGridSelect.FromSimpleGridCombat(
            choiceContext,
            candidates,
            player,
            prefs,
            rebuildCanonicalForRemoteApply: () => TributeSummonGridSelect.BuildStabilizedHandCandidates(player, null, null),
            PlayerChoiceOptions.CancelPlayCardActions);
        List<CardModel> ordered = picked.ToList();
        if (ordered.Count == 0)
            return;

        CardPile? drawPile = PileType.Draw.GetPile(player);
        if (drawPile == null)
            return;

        foreach (CardModel card in ordered)
        {
            if (card.Pile?.Type != PileType.Hand)
                continue;

            await CardPileCmd.Add(card, drawPile, CardPilePosition.Bottom, this, false);
        }
    }
}
