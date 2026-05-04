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

public sealed class Call_of_the_Haunted : BaseContinuousTrapCard, IYgoSpellTrapEquipLink, IYgoPrePlayCancelableGridSelection
{
    private static readonly YgoSearchPile[] SummonPiles =
    [
        YgoSearchPile.Graveyard
    ];

    private BaseMonsterCard? _equipLinkedMonster;
    private uint _equipLinkedPetCombatId;
    private BaseMonsterCard? _pendingLinkAfterZone;

    public Call_of_the_Haunted()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Trap;

    public override float PackWeightMultiplier => 1.3f;

    public override Type[] RelatedCards => new[]
    {
        typeof(Call_of_the_Haunted),
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
            _ => true);

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard) =>
        await YgoLinkedSpecialSummonSelection.TryPrepareSingleMonsterAsync(
            player,
            sourceCard,
            SummonPiles,
            SelectionScreenPrompt,
            _ => true);

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _pendingLinkAfterZone = await YgoLinkedSpecialSummonSelection.TryResolvePreparedSpecialSummonAsync(
            choiceContext,
            Owner,
            this,
            SummonPiles,
            _ => true);
    }

    protected override Task OnAfterContinuousTrapEnteredSpellTrapZoneAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        YgoLinkedSpecialSummonSelection.AttachLinkedTrapIfPending(this, ref _pendingLinkAfterZone);
        return Task.CompletedTask;
    }
}