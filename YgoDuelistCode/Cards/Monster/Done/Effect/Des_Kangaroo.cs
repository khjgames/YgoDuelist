using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Des_Kangaroo : EffectMonsterCard
{
    public Des_Kangaroo()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 15,
            baseDef: 17,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | FusionMonsterCard.PackTagsForFusionProfile(DuelMonsterAttribute, DuelMonsterRace);

    protected override async Task OnAfterMonsterPlayResolved(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        if (Type != CardType.Skill)
            return;

        await PowerCmd.Apply<YgoTemporaryThornsPower>(Owner.Creature, 2m, Owner.Creature, this);
    }
}
