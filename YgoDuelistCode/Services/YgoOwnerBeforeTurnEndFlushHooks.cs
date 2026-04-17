using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;

namespace YgoDuelist.YgoDuelistCode.Services;

public static class YgoOwnerBeforeTurnEndFlushHooks
{
    public static async Task DispatchAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        if (owner.PlayerCombatState != null)
        {
            foreach (Creature pet in owner.PlayerCombatState.Pets.ToList())
            {
                if (!pet.IsAlive)
                    continue;
                if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect hook)
                    continue;
                if (!hook.IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(pet))
                    continue;
                await hook.TryResolveOwnerBeforeTurnEndFlushFieldMonsterEffectAsync(choiceContext, owner, pet);
            }
        }

        CardPile? gy = GraveyardRelic.GetGraveyardPile(owner);
        if (gy != null)
        {
            foreach (CardModel card in gy.Cards.ToList())
            {
                if (card is not IYgoOwnerBeforeTurnEndFlushGraveyardEffect hook)
                    continue;
                if (!hook.IsOwnerBeforeTurnEndFlushGraveyardEffectActive())
                    continue;
                await hook.TryResolveOwnerBeforeTurnEndFlushGraveyardEffectAsync(choiceContext, owner);
            }
        }

        CardPile? banished = BanishedPile.CustomType.GetPile(owner);
        if (banished != null)
        {
            foreach (CardModel card in banished.Cards.ToList())
            {
                if (card is not IYgoOwnerBeforeTurnEndFlushBanishedEffect hook)
                    continue;
                if (!hook.IsOwnerBeforeTurnEndFlushBanishedEffectActive())
                    continue;
                await hook.TryResolveOwnerBeforeTurnEndFlushBanishedEffectAsync(choiceContext, owner);
            }
        }

        CardPile? zone = SpellTrapZonePile.CustomType.GetPile(owner);
        if (zone != null)
        {
            foreach (CardModel card in zone.Cards.ToList())
            {
                if (card is not IYgoOwnerBeforeTurnEndFlushSpellTrapZoneEffect hook)
                    continue;
                if (!hook.IsOwnerBeforeTurnEndFlushSpellTrapZoneEffectActive())
                    continue;
                await hook.TryResolveOwnerBeforeTurnEndFlushSpellTrapZoneEffectAsync(choiceContext, owner);
            }
        }
    }
}
