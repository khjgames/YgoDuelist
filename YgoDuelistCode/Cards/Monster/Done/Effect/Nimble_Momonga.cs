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
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Battle death: heal your leader by {Mgc}, then optionally Special Summon up to 2 copies from deck face-down.</summary>
public sealed class Nimble_Momonga : EffectMonsterCard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-NIMBLE_MOMONGA.activate_effect");
    private static readonly LocString SummonPrompt = new("cards", "YGODUELIST-NIMBLE_MOMONGA.summon_from_deck");

    public Nimble_Momonga()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 10,
            baseDef: 1,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    protected override bool UsesBattleDeathGraveyardMark => true;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Earth | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Nimble_Momonga) };

    public override bool BundleGrantsExtraCopyOfSelf => true;

    public override Type[] BundledCards => new[] { typeof(Nimble_Momonga) };

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        if (from != MonsterPile.CustomType)
            return;
        if (!YgoBattleDeathMarkedCards.Consume(this))
            return;
        Player? player = Owner;
        if (player?.Creature == null)
            return;
        TaskHelper.RunSafely(RunBattleDeathAsync(player));
    }

    private async Task RunBattleDeathAsync(Player player)
    {
        decimal heal = DynamicVars["Mgc"].BaseValue;
        if (heal > 0m && player.Creature != null)
            await CreatureCmd.Heal(player.Creature, heal);

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            this,
            ActivatePrompt);
        if (ctx == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
                return;

            Nimble_Momonga? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                new CardSelectorPrefs(SummonPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true
                },
                () => BuildDeckCandidates(player));
            if (chosen == null)
                return;

            chosen.FaceDown = true;
            await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, ctx);
        }
    }

    private static List<Nimble_Momonga> BuildDeckCandidates(Player player) => YgoPlayerPiles
        .OrderedCardsOfTypeFromPiles<Nimble_Momonga>(player, YgoPlayerPiles.Draw)
        .Where(m => m.CanSummonDuelMonster)
        .ToList();

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 15m;
    }
}
