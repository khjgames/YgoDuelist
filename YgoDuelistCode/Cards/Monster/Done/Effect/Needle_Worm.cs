using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Needle_Worm : EffectMonsterCard, IMonsterFlipEffect
{
    public override int AttackPortionCount => 3;
    public Needle_Worm()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 7,
            baseDef: 6,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Insect | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Needle_Worm) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Needle_Worm || Owner?.PlayerCombatState == null)
            return;

        Player player = Owner;
        CardPile? draw = YgoPlayerPiles.Draw(player);
        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (draw == null || gy == null)
            return;

        int n = (int)DynamicVars["Mgc"].BaseValue;
        if (n <= 0)
            return;

        for (int i = 0; i < n; i++)
        {
            if (draw.IsEmpty)
                break;
            CardModel? top = draw.Cards.Count > 0 ? draw.Cards[0] : null;
            if (top == null)
                break;
            await CardPileCmd.Add(top, gy, CardPilePosition.Top, top, false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 7m;
    }
}
