using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Secret_Pass_to_the_Treasures : BaseSpellCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 10m), new DynamicVar("Mgc2", 50m) };

    public Secret_Pass_to_the_Treasures()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    /// <summary>Used by <see cref="Patches.PlayCardActionSecretPassPatch"/> to filter field monsters.</summary>
    public decimal AtkThresholdForSelection => DynamicVars["Mgc"].BaseValue;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner)
            .OfType<BaseMonsterCard>()
            .Any(m => m.CalcDuelMonsterStats(DuelMonsterFieldRegistry.GetFieldMonsters(Owner)).Atk <= AtkThresholdForSelection);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        if (!SecretPassPlayPayload.TryTakePending(this, out BaseMonsterCard? targetMonster) || targetMonster == null)
            return;

        Creature? pet = Owner.PlayerCombatState.Pets.FirstOrDefault(p =>
            p.IsAlive
            && ReferenceEquals(DuelMonsterFieldRegistry.GetSourceCardForPet(p), targetMonster));

        if (pet == null)
            return;

        decimal pct = DynamicVars["Mgc2"].BaseValue;
        await PowerCmd.Apply<SecretPassTreasuresBlightPower>(pet, pct, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(10m);
        DynamicVars["Mgc2"].UpgradeValueBy(50m);
    }
}
