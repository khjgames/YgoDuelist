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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Burst_Stream_of_Destruction : BaseSpellCard
{
    private static readonly LocString BlueEyesSelectionPrompt =
        new("combat_messages", "BURST_STREAM_PICK_BLUE_EYES");

    public override bool UseAlternateUpgradedDescription => true;

    public Burst_Stream_of_Destruction()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.None, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner)
            .OfType<Cards.Monster.Todo.Normal.Blue_Eyes_White_Dragon>()
            .Any();

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Creature? targetCreature = cardPlay.Target;
        if (targetCreature == null || !targetCreature.IsAlive)
            return;

        if (DuelMonsterFieldRegistry.GetSourceCardForPet(targetCreature) is not Cards.Monster.Todo.Normal.Blue_Eyes_White_Dragon blueEyesCard)
            return;

        var fieldCards = DuelMonsterFieldRegistry.GetFieldMonsters(Owner).ToList();
        decimal dmg = blueEyesCard.CalcDuelMonsterStats(fieldCards).Atk;
        if (IsUpgraded)
            dmg *= 1.5m;

        foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive).ToList())
            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, Owner.Creature, this);
    }

    public static async Task<Creature?> PickBlueEyesOnFieldAsync(Player player, bool cancelable)
    {
        if (player.PlayerCombatState == null)
            return null;

        List<Creature> blueEyesPets = player.PlayerCombatState.Pets
            .Where(p => p.IsAlive
                && DuelMonsterFieldRegistry.GetSourceCardForPet(p) is Cards.Monster.Todo.Normal.Blue_Eyes_White_Dragon)
            .ToList();

        if (blueEyesPets.Count == 0)
            return null;
        if (blueEyesPets.Count == 1)
            return blueEyesPets[0];

        List<YgoEnemyIntentProxyCard> proxies = blueEyesPets.Select(c => new YgoEnemyIntentProxyCard(c)).ToList();
        var prefs = new CardSelectorPrefs(BlueEyesSelectionPrompt, 1, 1) { Cancelable = cancelable };
        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), proxies, player, prefs);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        YgoEnemyIntentProxyCard? pick = selected.OfType<YgoEnemyIntentProxyCard>().FirstOrDefault();
        Creature? chosen = pick?.TargetCreature;
        return chosen != null && chosen.IsAlive && blueEyesPets.Contains(chosen) ? chosen : null;
    }
}
