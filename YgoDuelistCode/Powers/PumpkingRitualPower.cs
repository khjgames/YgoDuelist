using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// On <see cref="Pumpking_the_King_of_Ghosts"/>: each of your turn starts, if you control a face-up <see cref="Castle_of_Dark_Illusions"/>, this duel monster gains 1 <see cref="NecroticEvolutionPower"/>, then loses 1 stack.
/// </summary>
public sealed class PumpkingRitualPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-PUMPKING_RITUAL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-PUMPKING_RITUAL_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-PUMPKING_RITUAL_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "pumpking_the_king_of_ghosts";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        Player? ownerPlayer = Owner.Player;
        Creature? playerCreature = ownerPlayer?.Creature;
        if (ownerPlayer == null || playerCreature == null)
            return;

        bool castleUp = DuelMonsterFieldRegistry.GetFieldMonsters(ownerPlayer)
            .Any(m => m is Castle_of_Dark_Illusions && !m.FaceDown);
        if (!castleUp)
            return;

        BaseMonsterCard? sourceCard = DuelMonsterFieldRegistry.GetSourceCardForPet(Owner) as BaseMonsterCard;

        if (Owner.GetPower<NecroticEvolutionPower>() is { } evo)
            await PowerCmd.ModifyAmount(evo, 1m, playerCreature, sourceCard);
        else
            await PowerCmd.Apply<NecroticEvolutionPower>(Owner, 1m, playerCreature, sourceCard);

        await PowerCmd.Decrement(this);
    }
}
