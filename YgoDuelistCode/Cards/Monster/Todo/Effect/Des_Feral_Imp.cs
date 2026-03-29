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
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Des_Feral_Imp : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipActivationPrompt =
        new("cards", "YGODUELIST-DES_FERAL_IMP.flip_preview.activation");

    private static readonly LocString FlipGraveyardPrompt =
        new("cards", "YGODUELIST-DES_FERAL_IMP.flip_preview.graveyard");

    public override bool UseAlternateUpgradedDescription => true;

    public Des_Feral_Imp()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 16,
            baseDef: 18,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Reptile)
    {
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Des_Feral_Imp || Owner == null || Owner.Creature?.CombatState == null)
            return;

        Player player = Owner;

        var activationPrefs = new CardSelectorPrefs(FlipActivationPrompt, 0, 0)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        await CardSelectCmd.FromSimpleGrid(choiceContext, new[] { self }, player, activationPrefs);

        int maxPick = (int)DynamicVars["Mgc"].BaseValue;
        if (maxPick <= 0)
            return;

        CardPile gravePile = GraveyardPile.CustomType.GetPile(player);
        List<CardModel> inGrave = gravePile.Cards.ToList();
        if (inGrave.Count == 0)
            return;

        int maxSelectable = System.Math.Min(maxPick, inGrave.Count);
        var gravePrefs = new CardSelectorPrefs(FlipGraveyardPrompt, 0, maxSelectable)
        {
            RequireManualConfirmation = true,
            Cancelable = false
        };
        IEnumerable<CardModel> picked = await CardSelectCmd.FromSimpleGrid(choiceContext, inGrave, player, gravePrefs);
        List<CardModel> toShuffle = picked.ToList();
        if (toShuffle.Count == 0)
            return;

        CardPile drawPile = PileType.Draw.GetPile(player);
        foreach (CardModel card in toShuffle)
            await CardPileCmd.Add(card, drawPile, CardPilePosition.Random, card, false);

        await CardPileCmd.Shuffle(choiceContext, player);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
