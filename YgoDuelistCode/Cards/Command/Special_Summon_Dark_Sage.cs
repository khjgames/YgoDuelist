using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Command on <see cref="Dark_Magician"/> after <see cref="Dark_Magician.SurvivedTimeMagic"/>:
/// Tribute this monster; Special Summon <see cref="Dark_Sage"/> from the hand, draw pile, or discard pile.
/// </summary>
public sealed class Special_Summon_Dark_Sage : MonsterCommandCard, IYgoNHandPlayPhaseHighlightOverride
{
    private static readonly LocString PickDarkSagePrompt = new("cards", "YGODUELIST-SPECIAL_SUMMON_DARK_SAGE.selection");

    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    public Special_Summon_Dark_Sage()
    {
    }

    protected override int CanonicalEnergyCost => 0;

    public override CardType Type => CardType.Skill;

    public override TargetType TargetType => TargetType.Self;

    public override string PortraitPath
    {
        get
        {
            if (IsCanonical)
                return ModelDb.Card<Dark_Sage>().PortraitPath;

            TryResolveSourceMonsterFromStoredPetId();
            return "card.png".CardImagePath();
        }
    }

    public Color? GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight)
    {
        _ = holder;
        if (Pile?.Type != YgoCardOptionPile.CustomType)
            return null;
        if (Owner == null)
            return null;
        try
        {
            if (!LocalContext.IsMe(Owner))
                return null;
        }
        catch
        {
            return null;
        }

        if (!vanillaWouldUseCyanPlayableHighlight)
            return null;

        return YgoNHandPlayPhaseHighlightColors.CallOfTheMummyYellow;
    }

    public static List<Dark_Sage> BuildDarkSageHandOrDeckCandidates(Player player) =>
        YgoPlayerPiles.OrderedCardsOfTypeFromHandDrawDiscard<Dark_Sage>(player);

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster == null || SourceMonster.FaceDown || Owner == null)
                return false;
            if (SourceMonster is not Dark_Magician dm || !dm.SurvivedTimeMagic)
                return false;
            if (BuildDarkSageHandOrDeckCandidates(Owner).Count == 0)
                return false;
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 1))
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            return pet != null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null || SourceMonster is not Dark_Magician source)
            return;
        Player player = Owner;

        if (!source.SurvivedTimeMagic)
            return;

        List<Dark_Sage> pool = BuildDarkSageHandOrDeckCandidates(player);
        if (pool.Count == 0)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        CardPile? monsterZone = YgoPlayerPiles.MonsterZone(player);
        CardPile? grave = YgoPlayerPiles.Graveyard(player);
        if (monsterZone == null || grave == null)
            return;

        Dark_Sage? picked = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickDarkSagePrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            },
            () => pool);

        if (picked == null || !pool.Contains(picked))
            return;

        CardPile? pickedPile = picked.Pile;
        if (pickedPile != YgoPlayerPiles.Hand(player)
            && pickedPile != YgoPlayerPiles.Draw(player)
            && pickedPile != YgoPlayerPiles.Discard(player))
            return;

        await DuelMonsterPetDeathPatch.MoveEquipsToGraveyardThenMonsterToPileAsync(
            player,
            source,
            monsterZone,
            grave);

        await CreatureCmd.Kill(pet, force: true);

        await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        YgoDarkSageSummonGate.EnterSummonBypass();
        bool summoned;
        try
        {
            summoned = await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, picked, choiceContext);
        }
        finally
        {
            YgoDarkSageSummonGate.ExitSummonBypass();
        }

        if (!summoned)
            return;

        Creature? sagePet = TributeSummonSelection.ResolvePetForFieldCard(player, picked);
        if (sagePet == null || !sagePet.IsAlive || player.Creature == null)
            return;

        if (!YgoStumblingField.IsActive(player))
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(sagePet, true, player.Creature, picked);
    }
}
