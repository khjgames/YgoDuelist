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
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>When destroyed by battle: you may select 1 Spell Card in your Graveyard and place it on top of your draw pile.</summary>
public sealed class Pixie_Knight : EffectMonsterCard
{
    private static readonly LocString SpellPrompt = new("cards", "YGODUELIST-PIXIE_KNIGHT.select_spell");

    public Pixie_Knight()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 13,
            baseDef: 2,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    protected override bool UsesBattleDeathGraveyardMark => true;

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        if (!YgoBattleDeathMarkedCards.Consume(this))
            return;

        Player? player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        TaskHelper.RunSafely(RunBattleToGraveyardAsync(player));
    }

    private async Task RunBattleToGraveyardAsync(Player player)
    {
        List<BaseSpellCard> candidates = BuildSpellCandidates(player);
        if (candidates.Count == 0)
            return;

        BlockingPlayerChoiceContext ctx = YgoChoiceContexts.Blocking();
        BaseSpellCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(SpellPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildSpellCandidates(player));
        if (chosen == null)
            return;

        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        CardPile? draw = YgoPlayerPiles.Draw(player);
        if (draw == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, draw, CardPilePosition.Top, chosen, false);
    }

    private static List<BaseSpellCard> BuildSpellCandidates(Player player) =>
        YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
            .OfType<BaseSpellCard>()
            .ToList();
}
