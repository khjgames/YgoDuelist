using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

/// <summary>While face-up: each time you resolve a Spell, a random enemy takes <c>{Mgc}</c> damage (see <see cref="YgoDuelist.YgoDuelistCode.Services.YgoCurseOfDarknessSpellHook"/>).</summary>
public sealed class Curse_of_Darkness : BaseContinuousTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 6m) };

    public Curse_of_Darkness()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Trap | YgoCardPackTags.Burn;

    
    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(3m);
}
