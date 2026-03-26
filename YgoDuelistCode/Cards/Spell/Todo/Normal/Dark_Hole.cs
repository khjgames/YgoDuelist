using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Dark_Hole : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 20m) };

    public Dark_Hole()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null || Owner.PlayerCombatState == null)
            return;

        decimal dmg = DynamicVars["Mgc"].BaseValue;

        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies.ToList())
        {
            if (!enemy.IsAlive)
                continue;
            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, Owner.Creature, this);
        }

        foreach (var pet in Owner.PlayerCombatState.Pets.ToList())
        {
            if (pet == null || !pet.IsAlive)
                continue;
            await CreatureCmd.Kill(pet, force: true);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(5m);
}
