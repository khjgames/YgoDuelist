using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Guardian_Sphinx : EffectMonsterCard, IMonsterActivatedEffect
{
    public Guardian_Sphinx()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 5,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 17,
            baseDef: 24,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Rock)
    {
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-GUARDIAN_SPHINX.activated_effect.description";
    public bool IsActivatedEffectAvailable => Owner != null && !FaceDown;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (Owner == null)
            return;
        var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, Owner);
        if (pet == null)
            return;

        FaceDown = true;
        await ApplyBattlePositionFromDuelCommandWithSwitchEffectsAsync(choiceContext, Owner, attackPosition: false);
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

}
