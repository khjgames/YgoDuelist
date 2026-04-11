using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>While Talisman of Trap Sealing is active: once per turn at turn start you may exhaust up to 2 Status/Curse cards from hand.</summary>
public sealed class TalismanTrapSealingFieldPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-TALISMAN_TRAP_SEALING_FIELD_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-TALISMAN_TRAP_SEALING_FIELD_POWER.description");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null || hand.Cards.Count == 0)
            return;

        int eligible = hand.Cards.Count(IsStatusOrCurse);
        if (eligible == 0)
            return;

        int maxPick = Math.Min(2, eligible);
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, maxPick)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        List<CardModel> candidates = TributeSummonGridSelect.BuildStabilizedHandCandidates(player, IsStatusOrCurse, null);

        var pick = await TributeSummonGridSelect.FromSimpleGridCombat(
            choiceContext,
            candidates,
            player,
            prefs,
            rebuildCanonicalForRemoteApply: () =>
                TributeSummonGridSelect.BuildStabilizedHandCandidates(player, IsStatusOrCurse, null),
            PlayerChoiceOptions.None);

        foreach (CardModel c in pick.ToList())
            await CardCmd.Exhaust(choiceContext, c);
    }

    private static bool IsStatusOrCurse(CardModel c) =>
        c.Type == CardType.Status || c.Type == CardType.Curse;
}
