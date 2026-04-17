using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Card-owned hook for effects that trigger when this card enters the player's YGO graveyard pile.
/// </summary>
public interface IYgoOnAddedToYgoGraveyardPile
{
    Task OnAddedToYgoGraveyardPileAsync(Player owner, CardPile pile);
}
