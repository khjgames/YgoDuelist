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

public sealed class Exarion_Universe : EffectMonsterCard, IMonsterActivatedEffect
{
    public Exarion_Universe()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 18,
            baseDef: 19,
            baseMgc: 4,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Exarion_Universe),
    };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-EXARION_UNIVERSE.activated_effect.description";

    public Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, source.Owner ?? cardPlay.Card?.Owner);
        if (pet == null)
            return Task.CompletedTask;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
        MonsterCommandRegistry.GetOrCreate(pet).ExarionUniversePiercingStanceThisTurn = true;
        return Task.CompletedTask;
    }

    public override bool AttackDealsSplinterDamage => ExarionPiercingStanceActive();

    public override bool CardShowsSplinterKeyword => true;

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (!ExarionPiercingStanceActive())
            return base.GetSecondaryStats();
        int penalty = (int)DynamicVars["Mgc"].BaseValue;
        return (-penalty, 0);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }

    private bool ExarionPiercingStanceActive()
    {
        if (Owner?.PlayerCombatState == null)
            return false;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(Owner.PlayerCombatState))
        {
            if (!DuelMonsterFieldRegistry.HasSourceCard(pet, this))
                continue;
            return MonsterCommandRegistry.TryGet(pet, out MonsterCommandState s) && s.ExarionUniversePiercingStanceThisTurn;
        }

        return false;
    }
}
