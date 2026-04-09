using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Menu-only card: closes the current monster options view.
/// </summary>
public sealed class Exit_Monster_Options : MonsterCommandCard
{
    // Parameterless ctor for reflection / scanners – not used at runtime.
    public Exit_Monster_Options()
    {
    }

    public Exit_Monster_Options(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    protected internal override string? CustomCommandEnergyTexturePath =>
        BaseFieldSpellCard.ActiveFaceUpZoneEnergyOrbPath;

    /// <summary>Shared by UI click and <see cref="GameActions.YgoMonsterMenuCommandGameAction"/> (MP).</summary>
    public static Task ExecuteExitAsync(Player player)
    {
        GD.Print("[ZGO] Exit_Monster_Options.ExecuteExitAsync() entered");
        if (player == null)
        {
            GD.Print("[ZGO_ERROR] Exit_Monster_Options.ExecuteExitAsync() early exit: player null");
            return Task.CompletedTask;
        }

        var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null)
        {
            GD.Print("[ZGO_ERROR] Exit_Monster_Options.ExecuteExitAsync() early exit: optionPile null");
            return Task.CompletedTask;
        }

        GD.Print($"[ZGO] Exit_Monster_Options: Clearing YgoCardOptionPile. Previous count={optionPile.Cards.Count}");
        YgoSecondHandSourceBridge.SetSource(player, YgoSecondHandSource.MonsterOptions);
        optionPile.Clear();
        YgoOptionHandBridge.SyncFromOptionPile(player);
        GD.Print("[ZGO] Exit_Monster_Options.ExecuteExitAsync() done");
        return Task.CompletedTask;
    }

    public Task OnClickedOption()
    {
        var player = Owner;
        if (player == null)
            return Task.CompletedTask;
        return ExecuteExitAsync(player);
    }
}
