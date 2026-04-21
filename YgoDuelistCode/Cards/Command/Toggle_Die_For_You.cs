using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.GameActions;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Toggles this monster's "Die for You" behavior: when enabled, it gains DieForYouPower and tracks the player's block.
/// </summary>
public sealed class Toggle_Die_For_You : MonsterCommandCard
{
    // For reflection / scanners
    public Toggle_Die_For_You()
    {
    }

    public Toggle_Die_For_You(NormalMonsterCard source)
        : base(source, 0, CardType.Skill, TargetType.Self)
    {
    }

    protected override bool IsPlayable => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<DieForYouPower>();
        }
    }

    internal override bool TryEnqueueUnplayableOptionPileMenu(Player player, Creature? target)
    {
        YgoMonsterMenuCommandNetHelper.TryEnqueueOrRunLocal(this, target);
        return true;
    }

    protected internal override string? CustomCommandEnergyTexturePath =>
        "YgoDuelist/images/card_frames/Invisible_Energy.png";

    /// <summary>Shared by UI click and <see cref="GameActions.YgoMonsterMenuCommandGameAction"/> (MP).</summary>
    public static async Task ExecuteToggleFromPetAsync(Player player, Creature pet)
    {
        if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not BaseMonsterCard sourceMonster)
            return;

        var state = MonsterCommandRegistry.GetOrCreate(pet);
        if (state.DieForYouForced)
            return;

        state.DieForYouEnabled = !state.DieForYouEnabled;

        if (state.DieForYouEnabled)
        {
            await PowerCmd.Apply<DieForYouPower>(pet, 1m, player.Creature, sourceMonster);
            var petNode = NCombatRoom.Instance?.GetCreatureNode(pet);
            petNode?.TrackBlockStatus(player.Creature);
        }
        else
            await PowerCmd.Remove<DieForYouPower>(pet);

        if (sourceMonster is BaseMonsterCard bm)
        {
            bm.AssertMutable();
            bm.YgoDieForYouUserToggleOn = state.DieForYouEnabled;
        }
    }

    public async Task OnClickedOption()
    {
        GD.Print("[ZGO] Toggle_Die_For_You.OnClickedOption() entered");
        var player = Owner;
        if (player == null || SourceMonster == null)
        {
            GD.Print("[ZGO_ERROR] Toggle_Die_For_You.OnClickedOption() early exit: player or SourceMonster null");
            return;
        }

        var pet = FindPetForMonster(SourceMonster, player);
        if (pet == null)
            return;

        await ExecuteToggleFromPetAsync(player, pet);

        GD.Print("[ZGO] Toggle_Die_For_You.OnClickedOption() done (option pile not cleared)");
    }

    private static Creature? FindPetForMonster(BaseMonsterCard source, Player player)
    {
        if (player.PlayerCombatState == null)
            return null;

        foreach (var pet in player.PlayerCombatState.Pets)
        {
            if (pet.Monster is DuelMonsterModel && DuelMonsterFieldRegistry.GetSourceCardForPet(pet) == source)
                return pet;
        }

        return null;
    }
}
