using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Universal YGO replay: one paid resolve, then sequential free duplicate spawns per replay count.
/// </summary>
internal static class YgoReplayCoordinator
{
    private static readonly HashSet<CardModel> ReplaySpawns = new();
    private static YgoReplayContext? _activeFirstPlay;

    public static bool ProcessingReplay { get; private set; }

    public static bool IsYgoCard(CardModel card) => card is IYgoCard;

    public static void MarkReplaySpawn(CardModel card) => ReplaySpawns.Add(card);

    public static bool IsReplaySpawn(CardModel card) => ReplaySpawns.Contains(card);

    public static void BeginFirstPlay(CardModel card, Creature? target)
    {
        if (card is not IYgoCard || ProcessingReplay || IsReplaySpawn(card))
            return;

        _activeFirstPlay = new YgoReplayContext
        {
            SourceCard = card,
            Player = card.Owner!,
            Route = ClassifyRoute(card),
            ReplayCount = 0,
            Target = target,
        };
    }

    /// <summary>
    /// Called from <see cref="MegaCrit.Sts2.Core.Hooks.Hook.ModifyCardPlayCount"/> postfix while first play is active.
    /// </summary>
    public static bool TryCapturePlayCount(CardModel card, ref int playCount)
    {
        if (card is not IYgoCard)
            return false;

        if (IsReplaySpawn(card) || ProcessingReplay)
        {
            playCount = 1;
            return true;
        }

        if (_activeFirstPlay == null || !ReferenceEquals(_activeFirstPlay.SourceCard, card))
        {
            playCount = 1;
            return true;
        }

        _activeFirstPlay.ReplayCount = Math.Max(0, playCount - 1);
        playCount = 1;
        return true;
    }

    public static void NoteSummonedOutput(BaseMonsterCard monster)
    {
        if (_activeFirstPlay?.Route != YgoReplayRoute.MonsterOutputOnly)
            return;

        _activeFirstPlay.SummonedOutputMonster = monster;
    }

    /// <summary>
    /// Records hand-summon attack/defend combat from the first paid <see cref="NormalMonsterCard.OnPlay"/> resolve.
    /// </summary>
    public static void NoteHandSummonCombatAction(NormalMonsterCard card, CardPlay cardPlay)
    {
        if (_activeFirstPlay == null || ProcessingReplay || IsReplaySpawn(card))
            return;
        if (_activeFirstPlay.Route != YgoReplayRoute.MonsterHandPlay)
            return;
        if (!ReferenceEquals(_activeFirstPlay.SourceCard, card))
            return;

        if (card.Type == CardType.Attack && cardPlay.Target != null)
            _activeFirstPlay.HandSummonCombat = YgoReplayHandSummonCombatKind.Attack;
        else if (card.Type == CardType.Skill)
            _activeFirstPlay.HandSummonCombat = YgoReplayHandSummonCombatKind.Defend;
    }

    /// <summary>
    /// Records whether the first hand summon granted immediate commands or applied stiff/fatigue.
    /// </summary>
    public static void NoteHandSummonCanAttackThisTurn(BaseMonsterCard card, bool canAttackThisTurn)
    {
        if (_activeFirstPlay == null || ProcessingReplay || IsReplaySpawn(card))
            return;
        if (_activeFirstPlay.Route != YgoReplayRoute.MonsterHandPlay)
            return;
        if (!ReferenceEquals(_activeFirstPlay.SourceCard, card))
            return;

        _activeFirstPlay.HandSummonCanAttackThisTurn = canAttackThisTurn;
    }

    public static async Task CompleteFirstPlayAndDrainAsync(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target)
    {
        if (_activeFirstPlay == null || !ReferenceEquals(_activeFirstPlay.SourceCard, card))
            return;

        YgoReplayContext context = FinalizeContext(_activeFirstPlay, target);
        _activeFirstPlay = null;

        if (context.ReplayCount <= 0)
            return;
        if (CombatManager.Instance.IsOverOrEnding)
            return;
        if (context.Player.Creature?.IsDead == true)
            return;

        ProcessingReplay = true;
        try
        {
            for (int i = 0; i < context.ReplayCount; i++)
            {
                if (CombatManager.Instance.IsOverOrEnding || context.Player.Creature?.IsDead == true)
                    break;

                switch (context.Route)
                {
                    case YgoReplayRoute.SpellTrap:
                        await YgoSpellTrapReplayFlow.ExecuteReplayAsync(choiceContext, context);
                        break;
                    case YgoReplayRoute.MonsterHandPlay:
                    case YgoReplayRoute.MonsterOutputOnly:
                        await YgoMonsterReplayFlow.ExecuteReplayAsync(choiceContext, context);
                        break;
                }
            }
        }
        finally
        {
            ProcessingReplay = false;
        }
    }

    public static void ClearCombatState()
    {
        _activeFirstPlay = null;
        ReplaySpawns.Clear();
        ProcessingReplay = false;
    }

    private static YgoReplayContext FinalizeContext(YgoReplayContext draft, Creature? target)
    {
        draft.Target = target;

        if (draft.SourceCard is BaseEquipSpellCard equip)
            draft.EquipTarget = equip.EquippedMonster;

        if (draft.Route == YgoReplayRoute.MonsterHandPlay && draft.SourceCard is BaseMonsterCard bm)
            draft.SummonedOutputMonster = bm;

        return draft;
    }

    private static YgoReplayRoute ClassifyRoute(CardModel card) =>
        card switch
        {
            BaseMonsterCard => YgoReplayRoute.MonsterHandPlay,
            FusionSpellCard or RitualSpellCard => YgoReplayRoute.MonsterOutputOnly,
            MonsterCommandCard => YgoReplayRoute.MonsterOutputOnly,
            BaseSpellCard or BaseTrapCard => YgoReplayRoute.SpellTrap,
            _ when card is IYgoCard => YgoReplayRoute.SpellTrap,
            _ => YgoReplayRoute.SpellTrap,
        };
}
