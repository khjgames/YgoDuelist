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

public sealed class Diffusion_Wave_Motion : BaseSpellCard
{
    private static readonly LocString SpellcasterSelectionPrompt =
        new("combat_messages", "DIFFUSION_WAVE_PICK_SPELLCASTER");

    public override bool UseAlternateUpgradedDescription => true;

    public Diffusion_Wave_Motion()
        : base(cost: 0, cardType: CardType.Attack, rarity: CardRarity.Common, target: TargetType.None, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Light | YgoCardPackTags.Spellcaster;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Any(IsLevelSevenPlusSpellcaster);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Creature? targetCreature = cardPlay.Target;
        if (targetCreature == null || !targetCreature.IsAlive)
            return;

        BaseMonsterCard? source = DuelMonsterFieldRegistry.GetSourceCardForPet(targetCreature);
        if (source == null || !IsLevelSevenPlusSpellcaster(source))
            return;

        var fieldCards = DuelMonsterFieldRegistry.GetFieldMonsters(Owner).ToList();
        decimal dmg = source.CalcDuelMonsterStats(fieldCards).Atk;
        if (IsUpgraded)
            dmg *= 1.5m;

        foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive).ToList())
            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, Owner.Creature, this);
    }

    public static bool IsLevelSevenPlusSpellcaster(BaseMonsterCard m) =>
        m.DuelMonsterRace == DuelMonsterRace.Spellcaster && m.GetEffectiveDuelMonsterLevel() >= 7;

    public static async Task<Creature?> PickLevelSevenSpellcasterOnFieldAsync(Player player, bool cancelable)
    {
        if (player.PlayerCombatState == null)
            return null;

        List<Creature> eligible = player.PlayerCombatState.Pets
            .Where(p => p.IsAlive
                && DuelMonsterFieldRegistry.GetSourceCardForPet(p) is BaseMonsterCard m
                && IsLevelSevenPlusSpellcaster(m))
            .ToList();

        if (eligible.Count == 0)
            return null;
        if (eligible.Count == 1)
            return eligible[0];

        List<YgoEnemyIntentProxyCard> proxies = eligible.Select(c => new YgoEnemyIntentProxyCard(c)).ToList();
        var prefs = new CardSelectorPrefs(SpellcasterSelectionPrompt, 1, 1) { Cancelable = cancelable };
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
        return chosen != null && chosen.IsAlive && eligible.Contains(chosen) ? chosen : null;
    }
}
