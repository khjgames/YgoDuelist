using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>
/// Implemented by monster cards that expose a field "Activate Effect" command.
/// The shared <see cref="Command.Activate_Effect"/> card reads cost, type, target, and localization from the source monster.
/// <see cref="OnActivatedEffect"/> must call <see cref="MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn"/> with
/// <c>true</c> once the effect is committed (same timing as when you used to call <c>SetHasUsedCommandThisTurn</c>).
/// </summary>
public interface IMonsterActivatedEffect
{
    int ActivatedEffectEnergyCost { get; }
    CardType ActivatedEffectCardType { get; }
    TargetType ActivatedEffectTarget { get; }

    /// <summary>Full cards.json key (e.g. <c>YGODUELIST-AMEBA.activated_effect.description</c>).</summary>
    string ActivatedEffectDescriptionLocKey { get; }

    /// <summary>Gates whether the shared <see cref="Command.Activate_Effect"/> option appears playable (e.g. need a spell in GY).</summary>
    bool IsActivatedEffectAvailable => true;

    Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source);
}

public static class MonsterActivatedEffectRuntime
{
    /// <param name="ownerFallback">When the source monster card's Owner is unset, use the Activate Effect command card's owner.</param>
    public static Creature? FindPetForSourceMonster(NormalMonsterCard source, Player? ownerFallback = null)
    {
        Player? player = source.Owner ?? ownerFallback;
        if (player?.PlayerCombatState == null)
            return null;

        return player.PlayerCombatState.Pets
            .FirstOrDefault(p => DuelMonsterFieldRegistry.GetSourceCardForPet(p) == source && p.IsAlive);
    }
}
