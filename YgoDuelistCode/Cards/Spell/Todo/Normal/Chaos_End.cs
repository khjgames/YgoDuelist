using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>
/// Damage uses the same <see cref="CalculatedDamageVar"/> pattern as <c>SoulStorm</c> (base + ExtraDamage × count): 0 + ExtraDamage × Shadow Realm cards.
/// </summary>
public sealed class Chaos_End : BaseSpellCard
{
    public override bool UsesCombatHandDescription => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new CalculationBaseVar(0m),
            new ExtraDamageVar(5m),
            new CalculatedDamageVar(ValueProp.Unpowered).WithMultiplier(ShadowRealmMultiplier)
        };

    public Chaos_End()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    /// <summary>Same structure as vanilla stack multipliers (e.g. SoulStorm × Souls): 0 outside combat hand preview path.</summary>
    private static decimal ShadowRealmMultiplier(CardModel card, Creature? _)
    {
        if (CombatManager.Instance?.IsInProgress != true || card.Owner?.PlayerCombatState == null)
            return 0m;
        var pile = YgoShadowRealmService.GetPile(card.Owner);
        return pile?.Cards.Count ?? 0;
    }

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && ShadowRealmHasAtLeastOne(Owner);

    private static bool ShadowRealmHasAtLeastOne(Player player)
    {
        var pile = YgoShadowRealmService.GetPile(player);
        return pile != null && pile.Cards.Count > 0;
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var pile = YgoShadowRealmService.GetPile(Owner);
        int n = pile?.Cards.Count ?? 0;
        decimal dmg = DynamicVars.ExtraDamage.BaseValue * n;
        if (dmg <= 0m)
            return;

        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies.ToList())
        {
            if (!enemy.IsAlive)
                continue;
            await CreatureCmd.Damage(choiceContext, enemy, dmg, ValueProp.Unpowered, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1m);
}
