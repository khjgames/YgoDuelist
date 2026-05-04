using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Rain_of_Mercy : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc2", 4m) };

    public Rain_of_Mercy()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Heal | YgoCardPackTags.Water;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState is not CombatState cs)
            return;

        decimal heal = DynamicVars["Mgc2"].BaseValue;

        foreach (Creature enemy in YgoMpCombatOrder.HittableEnemiesAliveOrderedByCombatId(cs))
            await CreatureCmd.Heal(enemy, heal);

        if (Owner.PlayerCombatState == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (pet == null || !pet.IsAlive)
                continue;
            if (pet.Monster is not DuelMonsterModel)
                continue;
            await CreatureCmd.Heal(pet, heal);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc2"].UpgradeValueBy(2m);
    }
}
