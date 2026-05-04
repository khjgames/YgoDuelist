using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>When sent to the Graveyard: optional activate, then target 1 face-up monster you control; it gains {Mgc} <see cref="GilferPowerPower"/>.</summary>
public sealed class Archfiend_of_Gilfer : EffectMonsterCard
{
    private static readonly LocString ActivatePrompt = new("cards", "YGODUELIST-ARCHFIEND_OF_GILFER.activate_effect");
    private static readonly LocString TargetPrompt = new("cards", "YGODUELIST-ARCHFIEND_OF_GILFER.select_monster");

    public Archfiend_of_Gilfer()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 22,
            baseDef: 25,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        Player? player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        TaskHelper.RunSafely(RunGilferFromGraveyardAsync(player, this));
    }

    private static async Task RunGilferFromGraveyardAsync(Player player, Archfiend_of_Gilfer selfInGraveyard)
    {
        if (BuildFieldTargets(player).Count == 0)
            return;

        PlayerChoiceContext? ctx = await YgoGraveyardTriggeredActivation.TryConfirmSourceAsync(
            player,
            selfInGraveyard,
            ActivatePrompt);
        if (ctx == null)
            return;

        BaseMonsterCard? target = await YgoOrderedCardSelection.TryChooseSingleAsync(
            ctx,
            player,
            new CardSelectorPrefs(TargetPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildFieldTargets(player));
        if (target == null)
            return;

        if (target is not NormalMonsterCard nmc)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(nmc, player);
        if (pet == null || player.Creature == null)
            return;

        decimal stacks = selfInGraveyard.DynamicVars["Mgc"].BaseValue;
        if (stacks <= 0m)
            return;

        await PowerCmd.Apply<GilferPowerPower>(pet, stacks, player.Creature, selfInGraveyard);
    }

    private static List<BaseMonsterCard> BuildFieldTargets(Player player) =>
        DuelMonsterFieldRegistry.OrderedFieldMonsters(player).Where(m => !m.FaceDown).ToList();

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 5m;
    }
}
