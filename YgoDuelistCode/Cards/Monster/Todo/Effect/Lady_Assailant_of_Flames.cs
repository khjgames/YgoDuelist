using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Lady_Assailant_of_Flames : EffectMonsterCard, IMonsterFlipEffect
{
    public Lady_Assailant_of_Flames()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 15,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Fire | YgoCardPackTags.Banish | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Lady_Assailant_of_Flames) };

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Lady_Assailant_of_Flames || Owner?.Creature?.CombatState == null)
            return;

        int banishCount = (int)DynamicVars["Mgc"].BaseValue;
        int blight = (int)DynamicVars["Mgc2"].BaseValue;
        if (banishCount > 0)
        {
            CardPile? draw = PileType.Draw.GetPile(Owner);
            for (int i = 0; i < banishCount; i++)
            {
                if (draw == null || draw.IsEmpty)
                    break;
                CardModel? top = draw.Cards.FirstOrDefault();
                if (top == null)
                    break;
                await YgoBanishedService.BanishCard(Owner, top);
            }
        }

        if (blight <= 0)
            return;
        foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies)
        {
            if (!enemy.IsAlive)
                continue;
            await PowerCmd.Apply<BlightPower>(enemy, blight, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 4m;
        DynamicVars["Mgc2"].BaseValue = 13m;
    }
}
