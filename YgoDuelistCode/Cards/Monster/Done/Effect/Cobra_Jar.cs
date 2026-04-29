using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Cobra_Jar : EffectMonsterCard, IMonsterFlipEffect
{
    public Cobra_Jar()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 6,
            baseDef: 3,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Reptile)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Earth | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Cobra_Jar), typeof(Poisonous_Snake_Token) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (Owner == null || self is not Cobra_Jar)
            return;
        await YgoTokenSummon.TrySpecialSummonTokenAsync<Poisonous_Snake_Token>(Owner, choiceContext, defensePosition: false);
    }
}
