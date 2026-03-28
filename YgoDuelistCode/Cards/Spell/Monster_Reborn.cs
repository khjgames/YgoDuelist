using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
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
public sealed class Monster_Reborn : BaseSpellCard
{

    public Monster_Reborn()
        : base(1, CardRarity.Rare, TargetType.Self, DuelMonsterRace.SpellNormal)
    {
    }

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

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
        // Match base game (see Eidolon): upgrade energy cost, not Cost field.
        EnergyCost.UpgradeBy(-1);
    }

    // Match base game pattern (see Clash): gate playability via IsPlayable.
    protected override bool IsPlayable =>
        base.IsPlayable &&
        Owner != null &&
        GraveyardRelic.GetGraveyardCards(Owner).Any(c => c is BaseMonsterCard) &&
        DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tributeReleaseCount: 0);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null)
            return;

        var graveyardMonsters = GraveyardRelic
            .GetGraveyardCards(player)
            .OfType<BaseMonsterCard>()
            .ToList();

        if (graveyardMonsters.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(
            SelectionScreenPrompt,
            1,
            1);

        var selected = await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            graveyardMonsters,
            player,
            prefs);

        var chosen = selected.FirstOrDefault() as BaseMonsterCard;
        if (chosen == null)
            return;

        // Summon the chosen monster for 0 energy cost; DuelMonsterSummon will also
        // move the card into the MonsterPile and out of Graveyard via CardPileCmd.Add.
        // Monster Reborn summons can use Attack/Defend the turn they are summoned.
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, choiceContext);
    }
}

