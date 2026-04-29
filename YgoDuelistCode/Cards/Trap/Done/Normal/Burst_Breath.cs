using System;
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
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

public sealed class Burst_Breath : BaseTrapCard
{
    public override bool UseAlternateUpgradedDescription => true;

    public Burst_Breath()
        : base(cost: 0, cardType: CardType.Attack, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Trap | YgoCardPackTags.Dragon | YgoCardPackTags.Burn;

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        var combat = player.Creature.CombatState;
        if (combat == null)
            return;

        var field = DuelMonsterFieldRegistry.OrderedFieldMonsters(player);
        List<BaseMonsterCard> dragons = BuildDragonFieldCandidates(player);

        if (dragons.Count == 0)
            return;

        BaseMonsterCard selectedDragon = dragons[0];
        if (dragons.Count > 1)
        {
            var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1)
            {
                Cancelable = true
            };

            selectedDragon = await YgoOrderedCardSelection.TryChooseSingleAsync(
                ctx,
                player,
                prefs,
                () => BuildDragonFieldCandidates(player));
            if (selectedDragon == null)
                return;
        }

        decimal dragonAtk = selectedDragon.CalcDuelMonsterStats(field).Atk;
        if (IsUpgraded)
            dragonAtk *= 1.5m;

        Creature? tributePet = TributeSummonSelection.ResolvePetForFieldCard(player, selectedDragon);
        if (tributePet == null || !tributePet.IsAlive)
            return;

        await CreatureCmd.Kill(tributePet, force: true);

        foreach (Creature e in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(combat))
        {
            await DamageCmd.Attack(dragonAtk)
                .FromCard(this)
                .Targeting(e)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    private static List<BaseMonsterCard> BuildDragonFieldCandidates(Player player) => DuelMonsterFieldRegistry
        .OrderedFieldMonsters(player)
        .Where(m => m.DuelMonsterRace == DuelMonsterRace.Dragon)
        .ToList();
}
