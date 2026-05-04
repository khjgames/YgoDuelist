using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.TrapMonster;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked;

public sealed class Metal_Reflect_Slime : BaseContinuousTrapCard, IYgoSpellTrapEquipLink
{
    private BaseMonsterCard? _equipLinkedMonster;
    private uint _equipLinkedPetCombatId;
    private BaseMonsterCard? _pendingLinkAfterZone;

    public Metal_Reflect_Slime()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.God;

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Metal_Reflect_Slime_Trap_Monster));

    public BaseMonsterCard? EquipLinkedMonster
    {
        get
        {
            YgoSpellTrapEquipLinkRegistry.TryRebindEquipLinkIfNeeded(this);
            return _equipLinkedMonster;
        }
    }

    public uint EquipLinkedPetCombatId => _equipLinkedPetCombatId;

    public void SetEquipLinkedMonster(BaseMonsterCard? monster) => _equipLinkedMonster = monster;

    public void SetEquipLinkedPetCombatId(uint petCombatId) => _equipLinkedPetCombatId = petCombatId;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tributeReleaseCount: 0);

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        var monster = player.Creature.CombatState.CreateCard<Metal_Reflect_Slime_Trap_Monster>(player);
        monster.SetBattlePositionFromDuelCommand(attackPosition: false);

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, monster, choiceContext))
            return;

        _pendingLinkAfterZone = monster;
    }

    protected override Task OnAfterContinuousTrapEnteredSpellTrapZoneAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (_pendingLinkAfterZone != null)
        {
            YgoSpellTrapEquipLinkRegistry.Attach(this, _pendingLinkAfterZone);
            _pendingLinkAfterZone = null;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reactor Slime: Set from hand, Deck, or Graveyard; can be activated this turn.
    /// </summary>
    public static async Task<bool> TrySetFromHandDeckOrGraveyardActivatableThisTurnAsync(
        Player player,
        PlayerChoiceContext choiceContext)
    {
        if (player?.Creature?.CombatState == null)
            return false;

        CardPile? zone = YgoPlayerPiles.SpellTrapZone(player);
        if (zone == null)
            return false;

        Metal_Reflect_Slime probe = player.Creature.CombatState.CreateCard<Metal_Reflect_Slime>(player);
        if (!YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(player, probe))
            return false;

        List<Metal_Reflect_Slime> candidates = BuildCandidates(player);
        if (candidates.Count == 0)
            return false;

        Metal_Reflect_Slime? pick;
        if (candidates.Count == 1)
        {
            pick = candidates[0];
        }
        else
        {
            var prefs = new CardSelectorPrefs(
                new LocString("cards", "YGODUELIST-REACTOR_SLIME.metal_reflect_select"),
                1,
                1)
            {
                Cancelable = true,
                RequireManualConfirmation = true,
            };

            pick = await YgoOrderedCardSelection.TryChooseSingleAsync(
                choiceContext,
                player,
                prefs,
                () => BuildCandidates(player));
        }

        if (pick == null)
            return false;

        pick.EnterSpellTrapZoneAsSetCard();
        pick.SetThisTurn = false;
        await CardPileCmd.Add(new[] { pick }, zone, CardPilePosition.Top, pick, false);
        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        YgoSpellTrapZoneAfterPlayUi.ScheduleSpellTrapSecondHandEnsureVisible(player);
        return true;
    }

    private static List<Metal_Reflect_Slime> BuildCandidates(Player player)
    {
        return YgoPlayerPiles.OrderedCardsOfTypeFromPiles<Metal_Reflect_Slime>(
            player,
            YgoPlayerPiles.Hand,
            YgoPlayerPiles.Draw,
            YgoPlayerPiles.Discard,
            YgoPlayerPiles.Graveyard);
    }
}
