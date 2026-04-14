using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Skelengel : EffectMonsterCard, IMonsterFlipEffect
{
    public Skelengel()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 9,
            baseDef: 4,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Skelengel) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Skelengel || Owner == null)
            return;
        int draw = (int)DynamicVars["Mgc"].BaseValue;
        if (draw <= 0)
            return;
        await CardPileCmd.Draw(choiceContext, draw, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
