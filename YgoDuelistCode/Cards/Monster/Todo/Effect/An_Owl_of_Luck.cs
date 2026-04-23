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
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

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
        List<BaseFieldSpellCard> fieldSpells = BuildFieldSpellTargets(Owner);
        if (fieldSpells.Count == 0)
            return;

        BaseFieldSpellCard? chosen = fieldSpells.Count == 1
            ? fieldSpells[0]
            : await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionPrompt, 1, 1) { Cancelable = true },
                () => BuildFieldSpellTargets(Owner));
        if (chosen == null)
            return;

        CardPile? draw = YgoPlayerPiles.Draw(Owner);
        if (draw == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, draw, CardPilePosition.Top, chosen, false);
    }

    private static List<BaseFieldSpellCard> BuildFieldSpellTargets(Player player)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        return draw == null
            ? []
            : YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards)
                .OfType<BaseFieldSpellCard>()
                .ToList();
    }
}
