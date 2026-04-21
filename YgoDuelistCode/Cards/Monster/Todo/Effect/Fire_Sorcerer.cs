using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>FLIP: banish 2 random cards from your hand; inflict all enemies with <c>Mgc</c> Blight.</summary>
public sealed class Fire_Sorcerer : EffectMonsterCard, IMonsterFlipEffect
{
    public Fire_Sorcerer()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 10,
            baseDef: 15,
            baseMgc: 8,
            duelMonsterRace: DuelMonsterRace.Spellcaster)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Fire | YgoCardPackTags.Spellcaster;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Fire_Sorcerer || Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        CardPile? hand = PileType.Hand.GetPile(Owner);
        if (hand == null)
            return;

        var pool = hand.Cards.ToList();
        for (int i = 0; i < 2 && pool.Count > 0; i++)
        {
            CardModel? pick = YgoDeterministicRng.PickOne(cs, pool, "FIRE_SORCERER_BANISH", (ulong)i);
            if (pick == null)
                break;
            pool.Remove(pick);
            await YgoBanishedService.BanishCard(Owner, pick);
        }

        int blight = (int)DynamicVars["Mgc"].BaseValue;
        if (blight <= 0)
            return;

        foreach (Creature enemy in cs.HittableEnemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<BlightPower>(enemy, blight, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 14m;
    }
}
