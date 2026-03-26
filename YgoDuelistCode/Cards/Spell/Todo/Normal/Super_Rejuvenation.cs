using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Super_Rejuvenation : BaseSpellCard
{
    /// <summary>Extra draw bonus on upgrade (mirrors <see cref="SuperRejuvenationPower"/>).</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 0m) };

    public Super_Rejuvenation()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        // Amount 1 is a placeholder so Apply runs; real draw count is set at end of this turn on the power.
        await PowerCmd.Apply<SuperRejuvenationPower>(Owner.Creature, 1, Owner.Creature, this);
    }
}
