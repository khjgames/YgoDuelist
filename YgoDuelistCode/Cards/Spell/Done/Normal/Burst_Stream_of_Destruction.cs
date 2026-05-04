using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Burst_Stream_of_Destruction
    : BaseSpellCard, IYgoNeowSignatureBurstStreamSpell, IYgoPlayCardActionPreSpendResourceFlow
{
    private static readonly LocString BlueEyesSelectionPrompt =
        new("combat_messages", "BURST_STREAM_PICK_BLUE_EYES");

    private static readonly Dictionary<CardModel, Creature> PendingResolvedTargets = new();
    private static readonly object PendingGate = new();

    public override bool UseAlternateUpgradedDescription => true;

    public override bool CancelSpellTrapZonePlayWhenUnresolvedTargetAfterResolve => true;

    public Burst_Stream_of_Destruction()
        : base(cost: 0, cardType: CardType.Attack, rarity: CardRarity.Uncommon, target: TargetType.None, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Light | YgoCardPackTags.Dragon | YgoCardPackTags.Bundled;

    public override Type[] BundledCards => new[] { typeof(Blue_Eyes_White_Dragon) };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner).Any(YgoMonsterArchetypeKeywords.IsFaceUpBlueEyesWhiteDragonArchetype);

    protected override Type[] PreviewReferencedCardTypes => new[] { typeof(Blue_Eyes_White_Dragon) };

    public async Task<bool> TryPreparePreSpendPlayAsync(PlayCardAction action, Player player, CardModel self)
    {
        Creature? targetFromAction = null;
        if (player.Creature?.CombatState != null)
            targetFromAction = await player.Creature.CombatState.GetCreatureAsync(action.TargetId, 10.0);

        Creature? resolved = await TryResolveSpellTrapZonePlayTargetAsync(
            player,
            targetFromAction,
            cancelable: true);

        if (resolved == null || !IsValidTargetForSpellTrapZonePlay(resolved))
            return false;

        lock (PendingGate)
            PendingResolvedTargets[self] = resolved;

        return true;
    }

    public void ClearPreSpendPlayState(CardModel self)
    {
        lock (PendingGate)
            PendingResolvedTargets.Remove(self);
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Creature? targetCreature = cardPlay.Target ?? TryPeekPendingResolvedTarget(this);
        if (targetCreature == null || !targetCreature.IsAlive)
            return;

        if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(targetCreature) is not BaseMonsterCard sourceMonster
            || !YgoMonsterArchetypeKeywords.IsFaceUpBlueEyesWhiteDragonArchetype(sourceMonster))
            return;

        var fieldCards = DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner);
        decimal dmg = sourceMonster.CalcDuelMonsterStats(fieldCards).Atk;
        if (IsUpgraded)
            dmg *= 1.5m;

        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(Owner.Creature.CombatState))
            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, Owner.Creature, this);
    }

    private static Creature? TryPeekPendingResolvedTarget(CardModel source)
    {
        lock (PendingGate)
            return PendingResolvedTargets.TryGetValue(source, out Creature target) ? target : null;
    }

    public static async Task<Creature?> PickBlueEyesOnFieldAsync(Player player, bool cancelable)
    {
        if (player.PlayerCombatState == null)
            return null;

        List<Creature> blueEyesPets = YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState)
            .Where(p => p.IsAlive
                && DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(p) is BaseMonsterCard bm
                && YgoMonsterArchetypeKeywords.IsFaceUpBlueEyesWhiteDragonArchetype(bm))
            .ToList();

        if (blueEyesPets.Count == 0)
            return null;

        if (blueEyesPets.Count == 1)
            return blueEyesPets[0];

        return await YgoCreatureProxySelection.TryChooseSingleCreatureAsync(
            YgoChoiceContexts.Blocking(),
            player,
            blueEyesPets,
            BlueEyesSelectionPrompt,
            cancelable);
    }

    public override async Task<Creature?> TryResolveSpellTrapZonePlayTargetAsync(Player player, Creature? targetFromAction, bool cancelable)
    {
        if (targetFromAction != null)
            return targetFromAction;

        return await PickBlueEyesOnFieldAsync(player, cancelable);
    }

    public override bool IsValidTargetForSpellTrapZonePlay(Creature? target)
    {
        if (target == null || !target.IsAlive || Owner?.Creature == null)
            return false;

        if (DuelMonsterFieldRegistry.GetSourceMonster<Blue_Eyes_White_Dragon>(target) is null)
            return false;

        return target.Side == Owner.Creature.Side;
    }
}