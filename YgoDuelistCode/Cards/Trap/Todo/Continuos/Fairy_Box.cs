using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Fairy_Box : BaseContinuousTrapCard
{
    public Fairy_Box()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        CombatState? cs = Owner.Creature.CombatState;
        if (cs == null)
            return;

        CardModel headsCall = ModelDb.Card<Second_Coin_Toss>();
        CardModel tailsCall = ModelDb.Card<Book_of_Moon>();
        var coinOptions = new List<CardModel> { headsCall, tailsCall };

        CardModel? callPick = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            coinOptions,
            Owner,
            canSkip: false);

        if (callPick != null)
        {
            bool calledHeads = callPick.Id.Entry == headsCall.Id.Entry;
            bool flipIsHeads = YgoDeterministicRng.CoinFlip(cs, "FAIRY_BOX-COIN", YgoDeterministicRng.MixSpellTrapZoneSlot(Owner, this));

            if (calledHeads == flipIsHeads)
            {
                foreach (Creature e in cs.HittableEnemies)
                {
                    if (e.IsAlive)
                        await PowerCmd.Apply<WeakPower>(e, 1m, Owner.Creature, this);
                }
            }
        }

        await PowerCmd.Apply<FairyBoxFieldPower>(Owner.Creature, 1m, Owner.Creature, this);
    }
}
