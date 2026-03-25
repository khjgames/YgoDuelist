using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Diffusion_Wave_Motion : BaseSpellCard
{
    public Diffusion_Wave_Motion()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.AnyAlly, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Any(IsLevelSevenPlusSpellcaster);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Creature? targetCreature = cardPlay.Target;
        if (targetCreature == null || !targetCreature.IsAlive)
            return;

        BaseMonsterCard? source = DuelMonsterFieldRegistry.GetSourceCardForPet(targetCreature);
        if (source == null || !IsLevelSevenPlusSpellcaster(source))
            return;

        var fieldCards = DuelMonsterFieldRegistry.GetFieldMonsters(Owner).ToList();
        decimal dmg = source.CalcDuelMonsterStats(fieldCards).Atk;

        foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive).ToList())
            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, Owner.Creature, this);
    }

    private static bool IsLevelSevenPlusSpellcaster(BaseMonsterCard m) =>
        m.DuelMonsterRace == DuelMonsterRace.Spellcaster && m.GetEffectiveDuelMonsterLevel() >= 7;
}
