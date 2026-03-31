using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Karate_Man : EffectMonsterCard, IMonsterActivatedEffect
{
    public Karate_Man()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 10,
            baseDef: 10,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Earth | YgoCardPackTags.Warrior;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Karate_Man),
    };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-KARATE_MAN.activated_effect.description";

    public Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, source.Owner ?? cardPlay.Card?.Owner);
        if (pet == null)
            return Task.CompletedTask;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
        MonsterCommandState state = MonsterCommandRegistry.GetOrCreate(pet);
        state.KarateManBurstAtkThisTurn = true;
        state.KarateManDestroyAtEndOfOwnerTurn = true;
        return Task.CompletedTask;
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (!KarateManBurstIsActive())
            return base.GetSecondaryStats();
        return ((int)DynamicVars["Mgc"].BaseValue, 0);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 15m;
    }

    private bool KarateManBurstIsActive()
    {
        if (Owner?.PlayerCombatState == null)
            return false;

        foreach (Creature pet in Owner.PlayerCombatState.Pets)
        {
            if (DuelMonsterFieldRegistry.GetSourceCardForPet(pet) != this)
                continue;
            return MonsterCommandRegistry.TryGet(pet, out MonsterCommandState s) && s.KarateManBurstAtkThisTurn;
        }

        return false;
    }
}
