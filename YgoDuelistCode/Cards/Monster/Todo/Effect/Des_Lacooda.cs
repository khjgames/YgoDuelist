using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Des_Lacooda : EffectMonsterCard, IMonsterFlipEffect, IMonsterActivatedEffect
{
    public Des_Lacooda()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 5,
            baseDef: 6,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Zombie)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Des_Lacooda) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Des_Lacooda || Owner == null)
            return;
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-DES_LACOODA.activated_effect.description";
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
