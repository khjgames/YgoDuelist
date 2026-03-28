using System.Collections.Generic;
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

public sealed class Sparks : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 2m) };

    public Sparks()
        : base(cost: 0, cardType: CardType.Attack, rarity: CardRarity.Common, target: TargetType.AnyEnemy, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override Type[] BundledCards => new[]
    {
        typeof(Sparks),
        typeof(Hinotama),
    };

    public override Type[] RelatedCards => new[]
    {
        typeof(Sparks),
        typeof(Hinotama),
    };

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var target = cardPlay.Target;
        if (target == null)
            return;

        decimal dmg = DynamicVars["Mgc"].BaseValue;
        await CreatureCmd.Damage(choiceContext, target, dmg, ValueProp.Unpowered, Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(4m);
}
