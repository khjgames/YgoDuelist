using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Reactor_Slime : EffectMonsterCard, IMonsterActivatedEffect
{
    public Reactor_Slime()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 5,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-REACTOR_SLIME.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null && YgoTokenSummon.MaxTokensThatFit(Owner) >= 2;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Reactor_Slime || Owner == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, Owner);
        if (pet == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0))
                break;
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Slime_Token>(Owner, choiceContext, defensePosition: false);
        }

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }
}
