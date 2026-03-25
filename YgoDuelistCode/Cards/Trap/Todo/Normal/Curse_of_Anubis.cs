using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

public sealed class Curse_of_Anubis : BaseTrapCard
{
    public Curse_of_Anubis()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null || Owner.PlayerCombatState == null)
            return;

        Player player = Owner;
        CombatState cs = player.Creature.CombatState;

        if (!player.Creature.HasPower<YgoCurseOfAnubisPlayerMarkerPower>())
            await PowerCmd.Apply<YgoCurseOfAnubisPlayerMarkerPower>(player.Creature, 1m, player.Creature, this);

        foreach (Creature pet in player.PlayerCombatState.Pets)
        {
            BaseMonsterCard? src = DuelMonsterFieldRegistry.GetSourceCardForPet(pet);
            if (src is not EffectMonsterCard)
                continue;
            if (pet.HasPower<YgoCurseOfAnubisEffectMonsterPower>())
                continue;
            await PowerCmd.Apply<YgoCurseOfAnubisEffectMonsterPower>(pet, 1m, player.Creature, this);
        }

        foreach (Creature enemy in cs.HittableEnemies)
        {
            if (!enemy.IsAlive)
                continue;
            await PowerCmd.Apply<StrengthPower>(enemy, -1m, player.Creature, this);
            await PowerCmd.Apply<DexterityPower>(enemy, -1m, player.Creature, this);
            await PowerCmd.Apply<WeakPower>(enemy, 1m, player.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}
