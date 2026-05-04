using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Contract_with_Exodia : BaseSpellCard
{
    public Contract_with_Exodia()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Dark | YgoCardPackTags.WinCon;

    public override Type[] RelatedCards => new[]
    {
        typeof(Contract_with_Exodia),
        typeof(Exodia_Necross),
        typeof(Exodia_the_Forbidden_One),
        typeof(Left_Arm_of_the_Forbidden_One),
        typeof(Right_Arm_of_the_Forbidden_One),
        typeof(Left_Leg_of_the_Forbidden_One),
        typeof(Right_Leg_of_the_Forbidden_One),
    };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && HasAllFiveForbiddenOnePiecesInGraveyard(Owner)
        && FindExodiaNecrossInHand(Owner) != null
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0);

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        if (!HasAllFiveForbiddenOnePiecesInGraveyard(player))
            return;

        Exodia_Necross? necross = FindExodiaNecrossInHand(player);
        if (necross == null)
            return;

        if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, necross, choiceContext);
    }

    private static Exodia_Necross? FindExodiaNecrossInHand(Player player) =>
        YgoPlayerPiles.OrderedCardsOfTypeFromPiles<Exodia_Necross>(player, YgoPlayerPiles.Hand).FirstOrDefault();

    private static bool HasAllFiveForbiddenOnePiecesInGraveyard(Player player)
    {
        var gy = YgoMpCombatOrder.CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player));
        return gy.OfType<Exodia_the_Forbidden_One>().Any()
            && gy.OfType<Left_Arm_of_the_Forbidden_One>().Any()
            && gy.OfType<Right_Arm_of_the_Forbidden_One>().Any()
            && gy.OfType<Left_Leg_of_the_Forbidden_One>().Any()
            && gy.OfType<Right_Leg_of_the_Forbidden_One>().Any();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
