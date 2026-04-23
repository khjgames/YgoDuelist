using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>
/// Central toggle for multiplayer investigation logs. Set <see cref="Verbose"/> to <c>true</c> in
/// <see cref="YgoDuelist.MainFile.Initialize"/> (or via debugger) for extra <see cref="GD.Print"/> lines during co-op.
/// Critical desync risks still use <see cref="GD.PrintErr"/> regardless of this flag.
/// </summary>
public static class YgoMpDiagnostics
{
    /// <summary>Extra non-error lines: action flow, pile snapshots, etc.</summary>
    public static bool Verbose;

    public static void VerbosePrint(string tag, string message)
    {
        if (!Verbose)
            return;
        GD.Print($"[YgoDuelist][MP][Diag][{tag}] {message}");
    }

    /// <summary>Where the card appears across piles when <see cref="CardModel.Pile"/> is unreliable (MP).</summary>
    public static string FormatPileMembership(Player player, CardModel card)
    {
        bool inHand = YgoPlayerPiles.Hand(player)?.Cards.Contains(card) == true;
        bool inStz = YgoPlayerPiles.SpellTrapZone(player)?.Cards.Contains(card) == true;
        bool inOpt = YgoPlayerPiles.OptionPile(player)?.Cards.Contains(card) == true;
        int pileType = (int)(card.Pile?.Type ?? 0);
        return $"pileType={pileType} inHandPile={inHand} inSpellTrapZone={inStz} inOptionPile={inOpt}";
    }

    public static bool IsMultiplayer =>
        RunManager.Instance?.NetService.Type != NetGameType.Singleplayer;
}
