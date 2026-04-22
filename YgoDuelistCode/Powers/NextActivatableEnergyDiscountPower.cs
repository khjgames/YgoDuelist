using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// Counter stacks: each stack makes one eligible Spell or Trap activation cost <see cref="EnergyDiscountPerActivation"/> less Energy; one stack is removed when that card is played (hand, Field Spell Zone, or chain).
/// </summary>
public abstract class NextActivatableEnergyDiscountPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected abstract decimal EnergyDiscountPerActivation { get; }

    protected abstract bool MatchesDiscountCard(CardModel card);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner)
            return false;
        if (!MatchesDiscountCard(card))
            return false;
        if (!IsSpellTrapActivatablePile(card.Pile?.Type))
            return false;
        if (Amount < 1m)
            return false;
        if (originalCost < 1m)
            return false;
        modifiedCost = originalCost - EnergyDiscountPerActivation;
        if (modifiedCost < 0m)
            modifiedCost = 0m;
        return modifiedCost < originalCost;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        CardModel card = cardPlay.Card;
        if (card.Owner?.Creature != Owner)
            return;
        if (!MatchesDiscountCard(card))
            return;
        if (!IsSpellTrapActivatablePile(card.Pile?.Type))
            return;
        if (Amount < 1m)
            return;

        Player? owner = card.Owner;
        if (owner?.Creature == null)
            return;

        await PowerCmd.ModifyAmount(this, -1m, owner.Creature, card);
        if (Amount <= 0m)
            await PowerCmd.Remove(this);
        YgoSpellTrapDiscountEnergyRefresh.ForPlayer(owner);
    }

    private static bool IsSpellTrapActivatablePile(PileType? pile) =>
        pile == PileType.Hand || pile == PileType.Play || pile == SpellTrapZonePile.CustomType;
}
