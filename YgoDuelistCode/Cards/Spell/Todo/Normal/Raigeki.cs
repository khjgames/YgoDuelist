using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using MegaCrit.Sts2.Core.Commands;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Raigeki : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 13m) };

    public Raigeki()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.AllEnemies, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        decimal blightAmount = DynamicVars["Mgc"].BaseValue;
        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<BlightPower>(enemy, blightAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(8m);
    }
}
