using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
        "YgoDuelist/images/card_frames/Invisible_Energy.png";

    public Task OnClickedOption()
    {
        GD.Print("[ZGO] Exit_Monster_Options.OnClickedOption() entered");
        var player = Owner;
        if (player == null)
        {
            GD.Print("[ZGO_ERROR] Exit_Monster_Options.OnClickedOption() early exit: player null");
            return Task.CompletedTask;
        }

        // Same clear pattern as DuelMonsterRightClickUiPatch.OpenMonsterOptions (builds options pile list).
        var optionPile = YgoCardOptionPile.CustomType.GetPile(player);
        if (optionPile == null)
        {
            GD.Print("[ZGO_ERROR] Exit_Monster_Options.OnClickedOption() early exit: optionPile null");
            return Task.CompletedTask;
        }

        GD.Print($"[ZGO] Exit_Monster_Options: Clearing YgoCardOptionPile. Previous count={optionPile.Cards.Count}");
        YgoSecondHandSourceBridge.SetSource(player, YgoSecondHandSource.MonsterOptions);
        optionPile.Clear();
        YgoOptionHandBridge.SyncFromOptionPile(player);
        GD.Print("[ZGO] Exit_Monster_Options.OnClickedOption() done");
        return Task.CompletedTask;
    }
}

