using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Breath_of_Light : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 5m), new DynamicVar("Mgc2", 1m) };

    protected override int CanonicalEnergyCost => 0;

    protected override bool HasEnergyCostX => true;

    public Breath_of_Light()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Spell;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        int x = ResolveEnergyXValue();
        decimal block = DynamicVars["Mgc"].BaseValue;
        decimal heal = DynamicVars["Mgc2"].BaseValue;

        for (int i = 0; i < x; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature, block, default, cardPlay);

            if (Owner.PlayerCombatState == null)
                continue;

            foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
            {
                if (pet == null || !pet.IsAlive || pet.Monster is not DuelMonsterModel)
                    continue;
                await CreatureCmd.Heal(pet, heal);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(3m);
        DynamicVars["Mgc2"].UpgradeValueBy(2m);
    }
}
