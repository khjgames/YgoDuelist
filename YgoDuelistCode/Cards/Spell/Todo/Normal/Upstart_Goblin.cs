using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Upstart_Goblin : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[]
        {
            new DynamicVar("Mgc", 2m),
            new DynamicVar("Mgc2", 10m)
        };

    public Upstart_Goblin()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        int draw = (int)DynamicVars["Mgc"].BaseValue;
        await CardPileCmd.Draw(choiceContext, draw, Owner);

        if (Owner.Creature?.CombatState == null)
            return;

        decimal heal = DynamicVars["Mgc2"].BaseValue;
        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
        {
            if (!enemy.IsAlive)
                continue;
            await CreatureCmd.Heal(enemy, heal);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);
}
