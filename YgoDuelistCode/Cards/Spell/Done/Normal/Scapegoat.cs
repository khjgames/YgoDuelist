using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;
using DuelMonsterSummon = YgoDuelist.YgoDuelistCode.Services.DuelMonsterSummon;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Scapegoat : BaseSpellCard
{
    public Scapegoat()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Spell | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Scapegoat), typeof(Sheep_Token) };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Sheep_Token));

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0)
        && ReactorSlimeSummonGate.AllowsSummonPrintedRace(Owner, DuelMonsterRace.Beast);

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return Task.CompletedTask;

        return SummonFourSheepAsync(choiceContext);
    }

    private async Task SummonFourSheepAsync(PlayerChoiceContext choiceContext)
    {
        for (int i = 0; i < 4; i++)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner!, 0))
                break;
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Sheep_Token>(Owner!, choiceContext, defensePosition: true);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
