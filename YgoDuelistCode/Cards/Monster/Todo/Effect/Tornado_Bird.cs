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
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Tornado_Bird : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipPrompt = new("cards", "YGODUELIST-TORNADO_BIRD.flip_return_spell_trap");

    public Tornado_Bird()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 11,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind;

    public override Type[] RelatedCards => new[] { typeof(Tornado_Bird) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Tornado_Bird || Owner == null)
            return;

        var candidates = new List<CardModel>();
        YgoFlipSpellTrapFieldEffects.CollectOwnerSpellAndTrapCardsInZone(Owner, candidates);
        if (candidates.Count == 0)
            return;

        IEnumerable<CardModel> pick;
        try
        {
            pick = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                Owner,
                new CardSelectorPrefs(FlipPrompt, 0, 2)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true
                });
        }
        catch (OperationCanceledException)
        {
            return;
        }

        foreach (CardModel c in pick.Distinct())
        {
            await YgoFlipSpellTrapFieldEffects.TryReturnSpellTrapFromZoneToHandAsync(Owner, c, self);
        }
    }
}
