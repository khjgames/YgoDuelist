using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Special Summoned. When this card attacks, heal your leader for {Mgc} HP.
/// </summary>
public sealed class Fushi_No_Tori : SpiritEffectMonsterCard
{
    public Fushi_No_Tori()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 12,
            baseDef: 0,
            baseMgc: 1,
            duelMonsterAttackPlayEnergyOverride: 1,
            duelMonsterRace: DuelMonsterRace.WingedBeast)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fire | YgoCardPackTags.Heal;

    public override Type[] RelatedCards => new[] { typeof(Fushi_No_Tori) };

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => false;

    public override async Task OnFirstUnblockedDamageToEnemyThisChainAsync(
        AttackCommand command,
        DamageResult r,
        Player atkPlayer,
        BlockingPlayerChoiceContext ctx)
    {
        _ = command;
        _ = ctx;
        int pastBlock = r.UnblockedDamage + r.OverkillDamage;
        if (atkPlayer.Creature == null || pastBlock <= 0)
            return;
        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(atkPlayer, this))
            return;
        int heal = (int)DynamicVars["Mgc"].BaseValue;
        if (heal > 0)
            await CreatureCmd.Heal(atkPlayer.Creature, heal);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
