using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

/// <summary>
/// Searches the draw or discard pile for Polymerization and adds it to the hand.
/// </summary>
public sealed class Fusion_Sage : BaseSpellCard
{
    private static readonly YgoSearchPile[] SearchPiles =
    [
        YgoSearchPile.Draw,
        YgoSearchPile.Discard
    ];

    public Fusion_Sage()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Fusion | YgoCardPackTags.Spell | YgoCardPackTags.Draw | YgoCardPackTags.Bundled;

    public override Type[] RelatedCards => new[] { typeof(Fusion_Sage), typeof(Polymerization) };

    public override Type[] BundledCards => new[] { typeof(Polymerization) };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && YgoPileSearchSelection.BuildCandidates<Polymerization>(Owner, SearchPiles).Count > 0;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null)
            return;

        await YgoPileSearchSelection.TrySearchToHandAsync<Polymerization>(
            player,
            SelectionScreenPrompt,
            SearchPiles,
            minSelect: 1,
            maxSelect: 1,
            cancelable: false,
            requireManualConfirmation: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}