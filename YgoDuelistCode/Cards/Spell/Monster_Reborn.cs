using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell;

/// <summary>
/// Monster Reborn – special summon 1 monster from your Graveyard for 0 energy.
/// Only playable when there is at least 1 monster in your Graveyard.
/// </summary>
public sealed class Monster_Reborn : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{

    public Monster_Reborn()
        : base(1, CardRarity.Rare, TargetType.Self, DuelMonsterRace.SpellNormal)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.4f;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Monster_Reborn),
    };

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override bool IsPlayable =>
        base.IsPlayable &&
        Owner != null &&
        BuildGraveyardMonsters(Owner).Count > 0 &&
        DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tributeReleaseCount: 0);

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<CardModel> graveyardMonsters = BuildGraveyardMonsters(player);

        if (graveyardMonsters.Count == 0)
            return false;

        return await YgoPrePlayGridSelection.TryPrepareSingleCardPayloadAsync<BaseMonsterCard>(
            player,
            sourceCard,
            graveyardMonsters,
            YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt),
            rebuildCanonicalForRemoteApply: () => BuildGraveyardMonsters(player));
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseMonsterCard chosen)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, choiceContext);
    }

    private static List<CardModel> BuildGraveyardMonsters(Player player) =>
        YgoMpCombatOrder
            .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .Where(ReactorSlimeSummonGate.SummonCandidatePredicate<BaseMonsterCard>(player))
            .Cast<CardModel>()
            .ToList();
}
