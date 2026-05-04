using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Insect_Princess : EffectMonsterCard
{
    private const int Mgc2Base = 5;

    public Insect_Princess()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 6,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 19,
            baseDef: 12,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Insect)
    {
    }

    public override Type[] RelatedCards => new[] { typeof(Insect_Princess), typeof(Insect_Queen) };

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Insect;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<InsectPrincessExecuteAtkPower>();
        }
    }

    public override StatEffectTotal GetStatEffect(BaseMonsterCard target)
    {
        if (Owner == null || target.DuelMonsterRace != DuelMonsterRace.Insect)
            return StatEffectTotal.None;

        int mgc = (int)DynamicVars["Mgc"].BaseValue;
        return new StatEffectTotal(mgc, mgc);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        base.CanonicalVars.Concat(new[] { new DynamicVar("Mgc2", Mgc2Base) });

    public override async Task OnEnemyExecutedByThisAttackAsync(AttackCommand command, CombatState cs)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;
        foreach (DamageResult r in command.Results)
        {
            if (r.Receiver.Side != CombatSide.Enemy || !r.WasTargetKilled)
                continue;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this);
            if (pet == null || !pet.IsAlive)
                continue;
            decimal stacks = DynamicVars["Mgc2"].BaseValue;
            if (stacks <= 0m)
                continue;
            await PowerCmd.Apply<InsectPrincessExecuteAtkPower>(pet, stacks, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2;
        DynamicVars["Mgc2"].BaseValue = Mgc2Base + 3;
    }
}
