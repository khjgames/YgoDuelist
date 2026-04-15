using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace YgoDuelist.YgoDuelistCode.Cards.Core;

/// <summary>Equip Spells that react when the equipped monster deals unblocked damage to an enemy (first time per enemy per attack chain).</summary>
public interface IEquipFirstUnblockedDamageEffect
{
    Task ApplyWhenEquippedMonsterDealsFirstUnblockedDamageAsync(
        BlockingPlayerChoiceContext ctx,
        Player atkPlayer,
        BaseMonsterCard equippedMonster,
        DamageResult hit);
}
