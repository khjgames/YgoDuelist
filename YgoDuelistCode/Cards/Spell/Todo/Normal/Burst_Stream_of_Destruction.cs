using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Services;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Burst_Stream_of_Destruction : BaseSpellCard
{
    public Burst_Stream_of_Destruction()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.AnyAlly, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner)
            .Any(m => m.Id.Entry.Contains("BLUE_EYES", StringComparison.OrdinalIgnoreCase));

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Creature? targetCreature = cardPlay.Target;
        if (targetCreature == null || !targetCreature.IsAlive)
            return;

        BaseMonsterCard? blueEyesCard = DuelMonsterFieldRegistry.GetSourceCardForPet(targetCreature);
        if (blueEyesCard == null)
            return;

        if (!blueEyesCard.Id.Entry.Contains("BLUE_EYES", StringComparison.OrdinalIgnoreCase))
            return;

        var fieldCards = DuelMonsterFieldRegistry.GetFieldMonsters(Owner).ToList();
        decimal dmg = blueEyesCard.CalcDuelMonsterStats(fieldCards).Atk;

        foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive).ToList())
            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, Owner.Creature, this);
    }
}
