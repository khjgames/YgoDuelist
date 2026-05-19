using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Effect monster with the Spirit Monster keyword: bounce to hand at owner turn end while face-up on the field.
/// </summary>
public abstract class SpiritEffectMonsterCard : EffectMonsterCard, IYgoSpiritMonster, IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect
{
    protected SpiritEffectMonsterCard(
        int cost,
        CardType type,
        CardRarity rarity,
        TargetType target,
        int duelMonsterLevel,
        DuelMonsterAttribute duelMonsterAttribute,
        int baseAtk,
        int baseDef,
        int baseMgc,
        DuelMonsterRace duelMonsterRace = DuelMonsterRace.Warrior,
        int? duelMonsterAttackPlayEnergyOverride = null,
        int? duelMonsterDefensePlayEnergyOverride = null)
        : base(
            cost,
            type,
            rarity,
            target,
            duelMonsterLevel,
            duelMonsterAttribute,
            baseAtk,
            baseDef,
            baseMgc,
            duelMonsterRace,
            duelMonsterAttackPlayEnergyOverride,
            duelMonsterDefensePlayEnergyOverride)
    {
    }

    public bool IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(Creature pet) =>
        Owner != null && !FaceDown && pet.IsAlive && DuelMonsterFieldRegistry.HasSourceCard(pet, this);

    public async Task TryResolveOwnerBeforeTurnEndFlushFieldMonsterEffectAsync(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature pet)
    {
        _ = choiceContext;
        _ = owner;
        if (!IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(pet))
            return;
        YgoDuelMonsterBounceToHand.RegisterForHandReturn(pet);
        await CreatureCmd.Kill(pet, force: true);
    }
}
