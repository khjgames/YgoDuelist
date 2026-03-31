using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

/// <summary>
/// Continuous. While face-up: monster commands cost +1 [E], your summons pay life equal to ⌈max HP / {Mgc}⌉ per command,
/// and each summon gets Replay 1 for Command Attack/Defend (non–dual-slot) and for Activate Effect separately.
/// Upgrade: cost [E] −1, {Mgc} 8 → 12.
/// </summary>
public sealed class Narrow_Pass : BaseContinuousTrapCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 8m) };

    public Narrow_Pass()
        : base(cost: 3, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Trap;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Narrow_Pass),
    };

    public override bool UseAlternateUpgradedDescription => true;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(4m);
    }

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override Task OnAfterContinuousTrapEnteredSpellTrapZoneAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player != null)
            MonsterCommandRegistry.RefillNarrowPassReplayChargesForPlayer(player);
        return Task.CompletedTask;
    }
}
