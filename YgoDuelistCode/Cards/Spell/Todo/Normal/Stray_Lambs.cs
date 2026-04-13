using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;
using DuelMonsterSummon = YgoDuelist.YgoDuelistCode.Services.DuelMonsterSummon;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Stray_Lambs : BaseSpellCard
{
    public Stray_Lambs()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Spell | YgoCardPackTags.Earth;

    public override Type[] RelatedCards => new[] { typeof(Stray_Lambs), typeof(Lamb_Token) };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Lamb_Token));

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return Task.CompletedTask;

        return SummonTwoLambsAsync(choiceContext);
    }

    private async Task SummonTwoLambsAsync(PlayerChoiceContext choiceContext)
    {
        for (int i = 0; i < 2; i++)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner!, 0))
                break;
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Lamb_Token>(Owner!, choiceContext, defensePosition: true);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
