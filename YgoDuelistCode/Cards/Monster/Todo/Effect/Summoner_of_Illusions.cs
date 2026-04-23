using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Summoner_of_Illusions : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    private static readonly LocString PickFusionPrompt =
        new("combat_messages", "FUSION_SUMMON_PICK_TARGET");

    public Summoner_of_Illusions()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 8,
            baseDef: 9,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    protected override bool StumblingBlocksHandSummonInAttackPosition => false;

    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Fusion;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Summoner_of_Illusions),
    };


    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Summoner_of_Illusions || Owner?.PlayerCombatState == null)
            return;

        Player player = Owner;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player,1))
            return;

        List<BaseMonsterCard> BuildTributeTargets() => TributeSummonSelection
            .BuildTributeCandidateCards(player)
            .Where(c => !ReferenceEquals(c, this))
            .ToList();

        List<BaseMonsterCard> tributeCandidates = BuildTributeTargets();

        List<FusionMonsterCard> fusionTargets = YgoFusionExtraDeckSelection.BuildFusionCardsInExtraDeck(player);
        if (tributeCandidates.Count == 0 || fusionTargets.Count == 0)
            return;

        BaseMonsterCard? tributeCard = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(TributePrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            BuildTributeTargets);
        if (tributeCard == null)
            return;

        Creature? tributePet = TributeSummonSelection.ResolvePetForFieldCard(player, tributeCard);
        if (tributePet == null || !tributePet.IsAlive)
            return;

        await CreatureCmd.Kill(tributePet, force: true);

        CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
        if (graveyard != null)
            await CardPileCmd.Add(new[] { tributeCard }, graveyard, CardPilePosition.Top, tributeCard, false);

        FusionMonsterCard? fusionCard = await YgoFusionExtraDeckSelection.TryChooseFusionFromExtraDeckAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickFusionPrompt, 1, 1) { Cancelable = true });
        if (fusionCard == null || !fusionTargets.Any(f => ReferenceEquals(f, fusionCard)))
            return;

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, fusionCard, choiceContext))
            return;

        Creature? fusionPet = TributeSummonSelection.ResolvePetForFieldCard(player, fusionCard);
        if (fusionPet == null || !fusionPet.IsAlive)
            return;

        await PowerCmd.Apply<YgoSummonerOfIllusionsFusionTimerPower>(
            fusionPet,
            1m,
            player.Creature,
            this);
    }

}
