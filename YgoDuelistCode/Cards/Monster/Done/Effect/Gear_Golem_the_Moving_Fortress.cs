using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Activate Effect: take printed Mgc blockable damage; this turn, this monster's attacks apply full Blight.</summary>
public sealed class Gear_Golem_the_Moving_Fortress : EffectMonsterCard, IMonsterActivatedEffect
{
    public Gear_Golem_the_Moving_Fortress()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 8,
            baseDef: 22,
            baseMgc: 8,
            duelMonsterRace: DuelMonsterRace.Machine,
            duelMonsterDefensePlayEnergyOverride: 2)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Earth | YgoCardPackTags.Machine | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Gear_Golem_the_Moving_Fortress) };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GEAR_GOLEM_THE_MOVING_FORTRESS.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner?.Creature?.CombatState != null;

    public override bool CardShowsBlightKeyword => true;

    public override bool AttackDealsBlightedDamage => true;

    public override bool AttackDealsFullBlightedDamage => FullBlightAttackThisTurn();

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.Creature == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        decimal selfDamage = source.DynamicVars["Mgc"].BaseValue;
        if (selfDamage > 0m)
        {
            await CreatureCmd.Damage(
                choiceContext,
                player.Creature,
                selfDamage,
                ValueProp.Move | ValueProp.Unpowered,
                dealer: null,
                cardSource: source);
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
        MonsterCommandRegistry.GetOrCreate(pet).GearGolemFullBlightAttacksThisTurn = true;
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 6m;
    }

    private bool FullBlightAttackThisTurn()
    {
        if (Owner?.PlayerCombatState == null)
            return false;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return MonsterCommandRegistry.TryGet(pet, out MonsterCommandState s) && s.GearGolemFullBlightAttacksThisTurn;
        }

        return false;
    }
}
