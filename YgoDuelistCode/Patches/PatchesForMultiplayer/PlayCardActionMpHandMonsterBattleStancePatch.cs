using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Patches.PatchesForMultiplayer;

/// <summary>
/// MP: <see cref="AbstractMonsterCard.Type"/> follows hand UI <c>_displayForm</c>. Only the acting peer runs targeting
/// UI before enqueue; observers often keep default defense/set visuals, so <see cref="Services.DuelMonsterStancePowerSync"/>
/// and <see cref="NormalMonsterCard.CombatAction"/> diverge from the owner.
/// <para/>
/// Owner stance is replicated on <see cref="MegaCrit.Sts2.Core.GameActions.NetPlayCardAction"/> via
/// <see cref="NetPlayCardActionHandMonsterStanceSerializePatch"/> / <see cref="NetPlayCardActionHandMonsterStanceDeserializePatch"/>
/// into <see cref="YgoNetPlayCardHandStanceStash"/>. If no stash entry exists (should not happen for hand monsters with
/// matching mod versions), we fall back to <see cref="AbstractMonsterCard.RegisteredCardType"/>.
/// </summary>
[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
[HarmonyPriority(950)]
public static class PlayCardActionMpObserverAlignHandMonsterBattleStancePatch
{
    static void Prefix(PlayCardAction __instance)
    {
        if (CombatManager.Instance?.IsInProgress != true)
            return;

        try
        {
            if (__instance.Player == null || LocalContext.IsMe(__instance.Player))
                return;
        }
        catch
        {
            return;
        }

        CardModel? card = __instance.NetCombatCard.ToCardModelOrNull();
        if (card is not AbstractMonsterCard amc || card.Pile?.Type != PileType.Hand)
            return;

        uint idx = __instance.NetCombatCard.CombatCardIndex;
        if (YgoNetPlayCardHandStanceStash.TryTake(idx, out bool atk, out bool he, out bool fd, out bool ws))
        {
            amc.ApplyNetworkObserverHandPlayBattleState(atk, he, fd, ws);
            GD.Print(
                $"[YgoDuelist][MP][HandStance] Observer applied wire hand stance atk={atk} handEffect={he} faceDown={fd} willSet={ws} netIdx={idx} card={card.Id?.Entry}");
            return;
        }

        GD.PrintErr(
            $"[YgoDuelist][MP][HandStance] No wire stance for hand monster; using RegisteredCardType (desync risk) netIdx={idx} card={card.Id?.Entry}");
        bool alignAttack = amc.RegisteredCardType == CardType.Attack;
        amc.SetBattlePositionFromDuelCommand(alignAttack);
        GD.Print(
            $"[YgoDuelist][MP][HandStance] Observer aligned hand monster from RegisteredCardType: attack={alignAttack} netIdx={idx} card={card.Id?.Entry}");
    }
}
