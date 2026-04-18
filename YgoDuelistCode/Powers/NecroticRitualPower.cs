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
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>
/// On this duel monster (Castle of Dark Illusions): each of your turn starts, apply 2 <see cref="NecroticEvolutionPower"/> to each face-up Zombie-Type monster on your field, then lose 1 stack.
/// </summary>
public sealed class NecroticRitualPower : YgoDuelistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-NECROTIC_RITUAL_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-NECROTIC_RITUAL_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-NECROTIC_RITUAL_POWER.smartDescription";

    protected override string? CardPortraitStemOverride => "castle_of_dark_illusions";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        Player? ownerPlayer = Owner.Player;
        PlayerCombatState? pcs = ownerPlayer?.PlayerCombatState;
        Creature? playerCreature = ownerPlayer?.Creature;
        if (pcs == null || playerCreature == null)
            return;

        BaseMonsterCard? sourceCard = DuelMonsterFieldRegistry.GetSourceCardForPet(Owner) as BaseMonsterCard;

        foreach (Creature pet in pcs.Pets.ToList())
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) is not BaseMonsterCard bm)
                continue;
            if (bm.FaceDown || bm.DuelMonsterRace != DuelMonsterRace.Zombie)
                continue;

            if (pet.GetPower<NecroticEvolutionPower>() is { } evo)
                await PowerCmd.ModifyAmount(evo, 2m, playerCreature, sourceCard);
            else
                await PowerCmd.Apply<NecroticEvolutionPower>(pet, 2m, playerCreature, sourceCard);
        }

        await PowerCmd.Decrement(this);
    }
}
