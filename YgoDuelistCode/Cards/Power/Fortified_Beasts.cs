using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Power;

public sealed class Fortified_Beasts : BaseYgoPowerCard<FortifiedBeastsPower>
{
    public override bool UseAlternateUpgradedDescription => true;

    public Fortified_Beasts()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    protected override async Task OnPowerPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        int stacks = IsUpgraded ? 3 : 2;
        await PowerCmd.Apply<FortifiedBeastsPower>(Owner.Creature, stacks, Owner.Creature, this);

        Player? player = Owner;
        if (player?.PlayerCombatState == null || player.Creature == null)
            return;

        YgoDuelistPassivePowerState.AddFortifiedBeastsBonus(player, stacks);
        await FortifiedBeastsDuelMonsterHp.SyncAllPlayerDuelMonstersAsync(player);
    }
}
