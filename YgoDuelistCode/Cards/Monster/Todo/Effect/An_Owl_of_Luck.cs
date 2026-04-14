using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class An_Owl_of_Luck : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString SelectionPrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public An_Owl_of_Luck()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 3,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(An_Owl_of_Luck) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not An_Owl_of_Luck || Owner == null)
            return;
        CardPile? draw = PileType.Draw.GetPile(Owner);
        if (draw == null)
            return;

        List<BaseFieldSpellCard> fieldSpells = draw.Cards.OfType<BaseFieldSpellCard>().ToList();
        if (fieldSpells.Count == 0)
            return;

        BaseFieldSpellCard chosen = fieldSpells[0];
        if (fieldSpells.Count > 1)
        {
            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                fieldSpells.Cast<CardModel>().ToList(),
                Owner,
                new CardSelectorPrefs(SelectionPrompt, 1, 1) { Cancelable = true });
            BaseFieldSpellCard? pick = selected.OfType<BaseFieldSpellCard>().FirstOrDefault();
            if (pick == null)
                return;
            chosen = pick;
        }

        await CardPileCmd.Add(new[] { chosen }, draw, CardPilePosition.Top, chosen, false);
    }
}
