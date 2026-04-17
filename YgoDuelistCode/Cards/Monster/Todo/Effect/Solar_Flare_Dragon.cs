using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>With another Pyro on the field: +<c>Mgc</c> ATK/DEF. End of turn: <c>Mgc2</c> Blight on a random enemy (see <see cref="YgoDuelist.YgoDuelistCode.Services.YgoSolarFlareDragonEndPhase"/>).</summary>
public sealed class Solar_Flare_Dragon : EffectMonsterCard, IYgoOwnerBeforeTurnEndFlushFieldMonsterEffect
{
    public Solar_Flare_Dragon()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 15,
            baseDef: 10,
            baseMgc: 3,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Fire;

    public override Type[] RelatedCards => new[] { typeof(Solar_Flare_Dragon) };

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in base.CanonicalVars)
                yield return v;
            yield return new DynamicVar("Mgc2", 5m);
        }
    }

    protected override (int atk, int def) GetSecondaryStats()
    {
        if (Owner == null)
            return base.GetSecondaryStats();

        bool otherPyro = DuelMonsterFieldRegistry
            .GetFieldMonsters(Owner)
            .Any(m => m != null && !m.FaceDown && !ReferenceEquals(m, this) && m.DuelMonsterRace == DuelMonsterRace.Pyro);
        if (!otherPyro)
            return (0, 0);

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return (mgc, mgc);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
        DynamicVars["Mgc2"].BaseValue = 7m;
    }

    public bool IsOwnerBeforeTurnEndFlushFieldMonsterEffectActive(Creature pet) =>
        !FaceDown && pet.IsAlive && HasOtherPyroOnField(Owner!, this);

    public async Task TryResolveOwnerBeforeTurnEndFlushFieldMonsterEffectAsync(PlayerChoiceContext choiceContext, Player owner, Creature pet)
    {
        if (owner.Creature?.CombatState == null || FaceDown || Owner == null)
            return;
        if (!HasOtherPyroOnField(owner, this))
            return;
        int blight = (int)DynamicVars["Mgc2"].BaseValue;
        if (blight <= 0)
            return;
        List<Creature> enemies = owner.Creature.CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
            return;
        Creature? target = YgoDeterministicRng.PickOne(owner.Creature.CombatState, enemies, "SOLAR_FLARE_DRAGON_BLIGHT", (ulong)(pet.CombatId ?? 0u));
        if (target == null)
            return;
        await PowerCmd.Apply<BlightPower>(target, blight, owner.Creature, this);
    }

    private static bool HasOtherPyroOnField(Player player, Solar_Flare_Dragon self)
    {
        foreach (BaseMonsterCard? m in DuelMonsterFieldRegistry.GetFieldMonsters(player))
        {
            if (m == null || m.FaceDown || ReferenceEquals(m, self))
                continue;
            if (m.DuelMonsterRace == DuelMonsterRace.Pyro)
                return true;
        }

        return false;
    }
}
