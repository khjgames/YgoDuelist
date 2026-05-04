using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>FLIP: banish exactly 1 monster card from your own Graveyard.</summary>
public sealed class Witch_Doctor_of_Chaos : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipBanishPrompt =
        new("cards", "YGODUELIST-WITCH_DOCTOR_OF_CHAOS.flip_banish_selection");

    public Witch_Doctor_of_Chaos()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 5,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Banish;

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Witch_Doctor_of_Chaos || Owner == null)
            return;

        List<BaseMonsterCard> candidates = BuildOwnGraveyardMonsterCandidates(Owner);
        if (candidates.Count == 0)
            return;

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(FlipBanishPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = false,
            },
            () => BuildOwnGraveyardMonsterCandidates(Owner));
        if (chosen == null || !YgoPlayerPiles.GraveyardContains(Owner, chosen))
            return;

        await YgoBanishedService.BanishCard(Owner, chosen);
    }

    private static List<BaseMonsterCard> BuildOwnGraveyardMonsterCandidates(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseMonsterCard>()
            .ToList();
}
