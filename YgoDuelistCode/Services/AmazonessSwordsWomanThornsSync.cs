using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary><see cref="Amazoness_Swords_Woman"/>: gain <see cref="ThornsPower"/> while face-up; strip when face-down or when the pet leaves the field.</summary>
public static class AmazonessSwordsWomanThornsSync
{
    public static async Task SyncForPetAsync(Creature pet, AbstractMonsterCard card, Creature? applier, CardModel? sourceCard)
    {
        if (card is not Amazoness_Swords_Woman aws || applier == null)
            return;

        if (card.FaceDown)
        {
            if (aws.PendingThornsOnPlayer > 0m)
                await StripThornsAsync(applier, aws);
            return;
        }

        if (aws.PendingThornsOnPlayer > 0m || aws.ThornsGrantExhaustedForThisField)
            return;

        decimal amt = aws.DynamicVars["Mgc"].BaseValue;
        if (amt <= 0m)
            return;

        await PowerCmd.Apply<ThornsPower>(applier, amt, applier, aws);
        aws.PendingThornsOnPlayer = amt;
        aws.ThornsGrantExhaustedForThisField = true;
    }

    public static async Task StripBeforeFieldUnregisterAsync(Amazoness_Swords_Woman aws, Creature playerCreature)
    {
        if (aws.PendingThornsOnPlayer > 0m)
            await StripThornsAsync(playerCreature, aws);
    }

    private static async Task StripThornsAsync(Creature playerCreature, Amazoness_Swords_Woman aws)
    {
        decimal n = aws.PendingThornsOnPlayer;
        aws.PendingThornsOnPlayer = 0m;
        if (n <= 0m)
            return;

        ThornsPower? t = playerCreature.GetPower<ThornsPower>();
        if (t == null)
            return;

        await PowerCmd.ModifyAmount(t, -n, playerCreature, aws);
    }
}
