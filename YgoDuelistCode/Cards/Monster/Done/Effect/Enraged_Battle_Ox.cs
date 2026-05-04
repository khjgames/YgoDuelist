using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>While face-up on the field, you gain Enraged Battle Ox power; your Beast-Warrior monsters deal splinter damage.</summary>
public sealed class Enraged_Battle_Ox : EffectMonsterCard, IMonsterFlipEffect
{
    public Enraged_Battle_Ox()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 17,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.BeastWarrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Enraged_Battle_Ox) };

    protected internal override async Task OnSummoned(Player player, PlayerChoiceContext choiceContext, Creature duelMonsterPet) =>
        await RunOnSummonedAsync(
            player,
            choiceContext,
            duelMonsterPet,
            () => SyncPlayerPowerAsync(player));

    public override async Task OnSwitchedFromDefenseToAttackFromCommandAsync(PlayerChoiceContext choiceContext, Player player)
    {
        await base.OnSwitchedFromDefenseToAttackFromCommandAsync(choiceContext, player);
        await SyncPlayerPowerAsync(player);
    }

    public override async Task OnSwitchedFromAttackToDefenseFromCommandAsync(PlayerChoiceContext choiceContext, Player player)
    {
        await base.OnSwitchedFromAttackToDefenseFromCommandAsync(choiceContext, player);
        await SyncPlayerPowerAsync(player);
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Enraged_Battle_Ox || Owner == null)
            return;
        await SyncPlayerPowerAsync(Owner);
    }

    public override Task OnAfterDuelMonsterPetDeathBeforeUnregisterAsync(Player player) =>
        SyncPlayerPowerAsync(player);

    private static Task SyncPlayerPowerAsync(Player player) =>
        EnragedBattleOxService.SyncPlayerPowerAsync(player);
}
