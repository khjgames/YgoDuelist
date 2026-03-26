using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Rush_Recklessly : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 7m) };

    public Rush_Recklessly()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Any();

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null)
            return;

        if (!RushReliablePlayPayload.TryTakePending(this, out var targetMonster) || targetMonster == null)
            return;

        Creature? targetPet = Owner.PlayerCombatState.Pets
            .FirstOrDefault(p => p.IsAlive && ReferenceEquals(DuelMonsterFieldRegistry.GetSourceCardForPet(p), targetMonster));
        if (targetPet == null)
            return;

        await PowerCmd.Apply<RushRecklesslyPower>(targetPet, DynamicVars["Mgc"].BaseValue, Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
