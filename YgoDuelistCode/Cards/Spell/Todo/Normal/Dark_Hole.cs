using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Dark_Hole : BaseSpellCard
{
    public Dark_Hole()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null || Owner.PlayerCombatState == null)
            return;

        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies.ToList())
        {
            if (!enemy.IsAlive)
                continue;
            await CreatureCmd.Damage(choiceContext, enemy, 20m, ValueProp.Unpowered, Owner.Creature, this);
        }

        foreach (var pet in Owner.PlayerCombatState.Pets.ToList())
        {
            if (pet == null || !pet.IsAlive)
                continue;
            await CreatureCmd.Kill(pet, force: true);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}
