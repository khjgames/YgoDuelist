using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Burning_Algae : EffectMonsterCard
{
    public Burning_Algae()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 5,
            baseDef: 15,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }

    public override async Task OnPetDiedBeforeOptionPileHandlingAsync(DuelMonsterPetDeathContext ctx)
    {
        await base.OnPetDiedBeforeOptionPileHandlingAsync(ctx);
        CombatState? cs = ctx.Pet.CombatState;
        if (cs == null)
            return;
        foreach (Creature enemy in cs.Enemies.Where(e => e.IsAlive))
            await CreatureCmd.Heal(enemy, 10m);
    }

    protected override void OnUpgrade() => base.OnUpgrade();
}
