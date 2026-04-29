using System;
using System.Collections.Generic;
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

/// <summary>Battle death: optionally destroy one of your field monsters, then gain 1 Conduit (and 1 Energy when upgraded).</summary>
public sealed class Newdoria : EffectMonsterCard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-NEWDORIA.activate_effect");
    private static readonly LocString DestroyPrompt = new("cards", "YGODUELIST-NEWDORIA.select_destroy");

    public Newdoria()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 12,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    protected override bool UsesBattleDeathGraveyardMark => true;

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Newdoria) };

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
        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            this,
            ActivatePrompt);
        if (ctx == null)
            return;

        await PlayerCmd.GainStars(1, player);
        if (IsUpgradedOrPreviewActive)
            await PlayerCmd.GainEnergy(1, player);

        if (player.PlayerCombatState == null)
            return;

        BaseMonsterCard? destroy = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(DestroyPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildDestroyCandidates(player));
        if (destroy == null)
            return;

        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            player.PlayerCombatState,
            p => DuelMonsterFieldRegistry.HasSourceCard(p, destroy));
        if (pet != null)
            await CreatureCmd.Kill(pet, force: true);
    }

    private static List<BaseMonsterCard> BuildDestroyCandidates(Player player) =>
        DuelMonsterFieldRegistry.OrderedFieldMonsters(player);

}
