using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>FLIP: Special Summon 1 "Gravekeeper's" monster with printed ATK 15 or less from the deck.</summary>
public sealed class Gravekeeper_s_Spy : EffectMonsterCard, IMonsterFlipEffect
{
    private const int MaxPrintedAtk = 15;
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-GRAVEKEEPER_S_SPY.flip_summon_select");

    public Gravekeeper_s_Spy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 12,
            baseDef: 20,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Spell | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Gravekeeper_s_Spy) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Gravekeeper_s_Spy || Owner == null)
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0))
            return;

        List<BaseMonsterCard> candidates = BuildDeckTargets(Owner);
        if (candidates.Count == 0)
            return;

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SummonPrompt, 1, 1) { Cancelable = true },
            () => BuildDeckTargets(Owner));
        if (chosen == null)
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(Owner, chosen, choiceContext);
    }

    private static List<BaseMonsterCard> BuildDeckTargets(Player player)
    {
        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return [];

        return YgoMpCombatOrder.CardsSnapshotOrderedForMp(draw.Cards)
            .OfType<BaseMonsterCard>()
            .Where(m => IsGravekeeperLowAtk(m) && m.CanSummonDuelMonster)
            .ToList();
    }

    private static bool IsGravekeeperLowAtk(BaseMonsterCard m) =>
        m.Id.Entry.Contains("GRAVEKEEPER", StringComparison.OrdinalIgnoreCase)
        && m.BaseAtk <= MaxPrintedAtk;
}
