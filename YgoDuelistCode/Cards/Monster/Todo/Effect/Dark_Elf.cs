using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>Whenever this card attacks from the field, you take printed <c>Mgc</c> blockable damage.</summary>
public sealed class Dark_Elf : EffectMonsterCard
{
    public Dark_Elf()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 20,
            baseDef: 8,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster;

    protected override async Task BeforeAttackCombatActionAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        Creature? pet = YgoMpCombatOrder.FirstPetWhere(
            Owner.PlayerCombatState,
            p => DuelMonsterFieldRegistry.HasSourceCard(p, this));
        if (pet == null || !pet.IsAlive)
            return;

        decimal dmg = DynamicVars["Mgc"].BaseValue;
        if (dmg <= 0m)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            dmg,
            ValueProp.Move | ValueProp.Unpowered,
            dealer: null,
            cardSource: this);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 7m;
    }
}
