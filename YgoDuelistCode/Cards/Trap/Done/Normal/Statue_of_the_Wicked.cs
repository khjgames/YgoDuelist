using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Normal;

public sealed class Statue_of_the_Wicked : BaseTrapCard, IYgoAfterFaceDownSetTrapDestroyedToGraveyardAsync
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[]
        {
            new DynamicVar("Mgc", 1m),
        };

    public Statue_of_the_Wicked()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Trap | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Statue_of_the_Wicked), typeof(Wicked_Token) };

    public async Task OnAfterFaceDownSetTrapDestroyedToGraveyardAsync(Player player)
    {
        if (player.Creature?.CombatState == null)
            return;
        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;
        var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
        await YgoTokenSummon.TrySpecialSummonTokenAsync<Wicked_Token>(player, ctx, defensePosition: false);
    }

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(1m);
    }
}
