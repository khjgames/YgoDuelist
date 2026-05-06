using System.Collections.Generic;
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
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Command on <see cref="Labyrinth_Wall"/> while equipped with face-up <see cref="Magical_Labyrinth"/>:
/// Tribute this monster; Special Summon <see cref="Wall_Shadow"/> from the hand, draw pile, or discard pile.
/// </summary>
public sealed class Special_Summon_Wall_Shadow : MonsterCommandCard, IYgoNHandPlayPhaseHighlightOverride
{
    private static readonly LocString PickWallShadowPrompt = new("cards", "YGODUELIST-SPECIAL_SUMMON_WALL_SHADOW.selection");

    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    public Special_Summon_Wall_Shadow()
    {
    }

    protected override int CanonicalEnergyCost => 0;

    public override CardType Type => CardType.Skill;

    public override TargetType TargetType => TargetType.Self;

    public override string PortraitPath => ModelDb.Card<Wall_Shadow>().PortraitPath;

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

    public static List<Wall_Shadow> BuildWallShadowHandOrDeckCandidates(Player player) =>
        YgoPlayerPiles.OrderedSummonableMonstersFromHandDrawDiscard<Wall_Shadow>(player);

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster == null || SourceMonster.FaceDown || Owner == null)
                return false;
            if (SourceMonster is not Labyrinth_Wall wall)
                return false;
            if (!Magical_Labyrinth.IsFaceUpEquippedTo(wall))
                return false;
            if (BuildWallShadowHandOrDeckCandidates(Owner).Count == 0)
                return false;
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 1))
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            return pet != null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null || SourceMonster is not Labyrinth_Wall source)
            return;
        Player player = Owner;

        if (!Magical_Labyrinth.IsFaceUpEquippedTo(source))
            return;

        List<Wall_Shadow> pool = BuildWallShadowHandOrDeckCandidates(player);
        if (pool.Count == 0)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        CardPile? monsterZone = YgoPlayerPiles.MonsterZone(player);
        CardPile? grave = YgoPlayerPiles.Graveyard(player);
        if (monsterZone == null || grave == null)
            return;

        Wall_Shadow? picked = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(PickWallShadowPrompt, 1, 1)
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

        YgoMagicalLabyrinthWallShadowSummonState.EnterSummonBypass();
        bool summoned;
        try
        {
            summoned = await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, picked, choiceContext);
        }
        finally
        {
            YgoMagicalLabyrinthWallShadowSummonState.ExitSummonBypass();
        }

        if (!summoned)
            return;
    }
}
