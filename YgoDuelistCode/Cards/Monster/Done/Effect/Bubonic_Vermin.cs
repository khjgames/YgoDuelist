using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Bubonic_Vermin : EffectMonsterCard, IMonsterFlipEffect
{
    public Bubonic_Vermin()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 9,
            baseDef: 6,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Draw;

    public override Type[] RelatedCards => new[] { typeof(Bubonic_Vermin) };

    public override bool BundleGrantsExtraCopyOfSelf => true;

    public override Type[] BundledCards => new[] { typeof(Bubonic_Vermin) };
    
    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Bubonic_Vermin || Owner == null)
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0))
            return;

        CardPile? draw = YgoPlayerPiles.Draw(Owner);
        Bubonic_Vermin? copy = (draw == null ? null : YgoMpCombatOrder.FirstCardWhereStable(draw.Cards, c => c is Bubonic_Vermin bv && !ReferenceEquals(bv, this)) as Bubonic_Vermin);
        if (copy == null)
            return;
        copy.FaceDown = true;
        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(Owner, copy, choiceContext);
    }
}
