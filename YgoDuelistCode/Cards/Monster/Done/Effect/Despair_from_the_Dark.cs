using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

/// <summary>Field → Graveyard: you may Tribute 2 of your field monsters; Special Summon this card from your Graveyard.</summary>
public sealed class Despair_from_the_Dark : EffectMonsterCard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-DESPAIR_FROM_THE_DARK.activate_effect");
    private static readonly LocString TributePrompt = new("cards", "YGODUELIST-DESPAIR_FROM_THE_DARK.select_tributes");

    public Despair_from_the_Dark()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 8,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 28,
            baseDef: 30,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Zombie;
    public override Type[] RelatedCards => new[] { typeof(Despair_from_the_Dark) };

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        if (from != MonsterPile.CustomType)
            return;
        Player? player = Owner;
        if (player?.Creature == null)
            return;
        TaskHelper.RunSafely(RunFromFieldToGraveyardAsync(player));
    }

    private async Task RunFromFieldToGraveyardAsync(Player player)
    {
        List<BaseMonsterCard> tributeCandidates = BuildTributeCandidates(player);
        if (tributeCandidates.Count < 2)
            return;
        if (!YgoPlayerPiles.GraveyardContains(player, this))
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 2))
            return;

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            this,
            ActivatePrompt);
        if (ctx == null)
            return;

        List<BaseMonsterCard> tributes = await YgoOrderedCardSelection.TryChooseManyAsync(
            ctx,
            player,
            new CardSelectorPrefs(TributePrompt, 2, 2) { Cancelable = true },
            () => BuildTributeCandidates(player),
            maxResults: 2);
        if (tributes.Count < 2 || player.PlayerCombatState == null)
            return;

        foreach (BaseMonsterCard tribute in tributes)
        {
            Creature? pet = YgoMpCombatOrder.FirstPetWhere(
                player.PlayerCombatState,
                p => DuelMonsterFieldRegistry.HasSourceCard(p, tribute));
            if (pet != null)
                await CreatureCmd.Kill(pet, force: true);
        }

        if (!YgoPlayerPiles.GraveyardContains(player, this))
            return;
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, this, ctx);
    }

    private static List<BaseMonsterCard> BuildTributeCandidates(Player player) =>
        DuelMonsterFieldRegistry.OrderedFieldMonsters(player)
            .Where(m => m != null)
            .ToList();
}
