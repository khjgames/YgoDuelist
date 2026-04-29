using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked;

public sealed class Soul_Resurrection : BaseContinuousTrapCard, IYgoSpellTrapEquipLink, IYgoPrePlayCancelableGridSelection
{
    private static readonly YgoSearchPile[] SummonPiles =
    [
        YgoSearchPile.Graveyard
    ];

    private BaseMonsterCard? _equipLinkedMonster;
    private uint _equipLinkedPetCombatId;
    private BaseMonsterCard? _pendingLinkAfterZone;

    public Soul_Resurrection()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Normal | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[]
    {
        typeof(Soul_Resurrection),
    };

    public BaseMonsterCard? EquipLinkedMonster
    {
        get
        {
            YgoSpellTrapEquipLinkRegistry.TryRebindEquipLinkIfNeeded(this);
            return _equipLinkedMonster;
        }
    }

    public uint EquipLinkedPetCombatId => _equipLinkedPetCombatId;

    public void SetEquipLinkedMonster(BaseMonsterCard? monster) => _equipLinkedMonster = monster;

    public void SetEquipLinkedPetCombatId(uint petCombatId) => _equipLinkedPetCombatId = petCombatId;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override bool IsPlayable =>
        base.IsPlayable &&
        YgoLinkedSpecialSummonSelection.CanPlay(
            Owner,
            SummonPiles,
            c => c is NormalMonsterCard);

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard) =>
        await YgoLinkedSpecialSummonSelection.TryPrepareSingleMonsterAsync(
            player,
            sourceCard,
            SummonPiles,
            SelectionScreenPrompt,
            c => c is NormalMonsterCard);

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _pendingLinkAfterZone = await YgoLinkedSpecialSummonSelection.TryResolvePreparedSpecialSummonAsync(
            choiceContext,
            Owner,
            this,
            SummonPiles,
            c => c is NormalMonsterCard);
    }

    protected override Task OnAfterContinuousTrapEnteredSpellTrapZoneAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        YgoLinkedSpecialSummonSelection.AttachLinkedTrapIfPending(this, ref _pendingLinkAfterZone);
        return Task.CompletedTask;
    }
}