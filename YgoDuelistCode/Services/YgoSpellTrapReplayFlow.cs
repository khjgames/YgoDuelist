using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

internal static class YgoSpellTrapReplayFlow
{
    private const string EquipSpellDefaultSelectionKey = "YGODUELIST-EQUIP_SPELL_DEFAULT.selectionScreenPrompt";

    public static async Task ExecuteReplayAsync(
        PlayerChoiceContext choiceContext,
        YgoReplayContext context)
    {
        Player player = context.Player;
        CardModel source = context.SourceCard;

        if (player.Creature?.IsDead == true || CombatManager.Instance.IsOverOrEnding)
            return;

        CardModel clone = YgoReplayCloneFactory.CreatePersistentClone(source);

        if (!YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(player, clone))
        {
            await YgoReplayFizzle.SendToGraveyardAsync(player, clone);
            return;
        }

        if (!await TryEnterFaceUpSpellTrapZoneAsync(player, clone))
        {
            await YgoReplayFizzle.SendToGraveyardAsync(player, clone);
            return;
        }

        if (clone is BaseEquipSpellCard equip)
        {
            BaseMonsterCard? equipTarget = await ResolveEquipTargetAsync(choiceContext, player, equip, context.EquipTarget);
            if (equipTarget == null)
            {
                await YgoReplayFizzle.SendToGraveyardAsync(player, clone);
                return;
            }

            EquipSpellPlayPayload.SetPending(clone, equipTarget);
        }
        else if (clone is IYgoPrePlayCancelableGridSelection prePlay)
        {
            bool prepared = await prePlay.TryPreparePrePlayCancelableGridAsync(player, clone);
            if (!prepared)
            {
                await YgoReplayFizzle.SendToGraveyardAsync(player, clone);
                return;
            }
        }

        Creature? target = context.Target;
        if (clone is BaseSpellCard bs)
            target = await bs.TryResolveSpellTrapZonePlayTargetAsync(player, target, cancelable: true);

        if (clone is BaseSpellCard spell && target == null && spell.CancelSpellTrapZonePlayWhenUnresolvedTargetAfterResolve)
        {
            await YgoReplayFizzle.SendToGraveyardAsync(player, clone);
            return;
        }

        bool needsTarget = clone.TargetType == TargetType.AnyEnemy || clone.TargetType == TargetType.AnyAlly;
        if (needsTarget && target == null)
        {
            await YgoReplayFizzle.SendToGraveyardAsync(player, clone);
            return;
        }

        var resources = new ResourceInfo
        {
            EnergySpent = 0,
            EnergyValue = 0,
            StarsSpent = 0,
            StarValue = 0,
        };

        try
        {
            await clone.OnPlayWrapper(choiceContext, target, isAutoPlay: false, resources);
        }
        finally
        {
            EquipSpellPlayPayload.ClearForCard(clone);
        }

        YgoSpellTrapZoneAfterPlayUi.ScheduleCleanup(player, clone);
    }

    private static async Task<bool> TryEnterFaceUpSpellTrapZoneAsync(Player player, CardModel card)
    {
        CardPile? zonePile = YgoPlayerPiles.SpellTrapZone(player);
        if (zonePile == null)
            return false;

        switch (card)
        {
            case BaseFieldSpellCard fieldSpell:
            {
                CardModel? existingField = YgoMpCombatOrder.FirstCardWhereStable(
                    zonePile.Cards,
                    YgoSpellTrapZoneBridge.IsFieldSpell);
                if (existingField != null)
                {
                    CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
                    if (graveyard != null)
                    {
                        await CardPileCmd.Add(
                            new[] { existingField },
                            graveyard,
                            CardPilePosition.Top,
                            card,
                            false);
                    }
                }

                fieldSpell.MarkAsFaceUpFieldInZone();
                break;
            }
            case BaseEquipSpellCard equipSpell:
                equipSpell.PrepareFaceUpForZoneFromGraveyard();
                break;
            case BaseSpellCard spell:
                spell.YgoPrepareFaceUpContinuousFromCardEffect();
                break;
            case BaseTrapCard trap:
                trap.MarkResolvingFaceUpInSpellTrapZone();
                break;
            default:
                return false;
        }

        await CardPileCmd.Add(
            new CardModel[] { card },
            zonePile,
            CardPilePosition.Top,
            card,
            false);

        YgoSpellTrapZoneBridge.SyncFromZonePile(player);
        return true;
    }

    private static async Task<BaseMonsterCard?> ResolveEquipTargetAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        BaseEquipSpellCard equip,
        BaseMonsterCard? defaultTarget)
    {
        var candidates = DuelMonsterFieldRegistry.OrderedFieldMonsters(player)
            .Where(m => YgoEquipSpellTargetRules.IsLegalEquipTarget(equip, m))
            .Cast<CardModel>()
            .ToList();

        if (candidates.Count == 0)
            return null;

        if (candidates.Count == 1)
            return candidates[0] as BaseMonsterCard;

        LocString prompt = ResolveEquipSelectionPrompt(equip);
        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(prompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await EquipSpellGridSelect.FromSimpleGrid(
                YgoChoiceContexts.Blocking(),
                candidates,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            if (defaultTarget != null
                && candidates.Contains(defaultTarget)
                && YgoEquipSpellTargetRules.IsLegalEquipTarget(equip, defaultTarget))
            {
                return defaultTarget;
            }

            return null;
        }

        return YgoMpCombatOrder.FirstCardWhereStable(selected, c => c is BaseMonsterCard) as BaseMonsterCard;
    }

    private static LocString ResolveEquipSelectionPrompt(BaseEquipSpellCard equip)
    {
        string cardKey = equip.Id.Entry + ".selectionScreenPrompt";
        return LocString.Exists("cards", cardKey)
            ? new LocString("cards", cardKey)
            : new LocString("cards", EquipSpellDefaultSelectionKey);
    }
}
