using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.TrapMonster;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked;

public sealed class The_First_Monarch : BaseContinuousTrapCard, IYgoSpellTrapEquipLink
{
    private BaseMonsterCard? _equipLinkedMonster;
    private uint _equipLinkedPetCombatId;
    private BaseMonsterCard? _pendingLinkAfterZone;

    public The_First_Monarch()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Trap;

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(The_First_Monarch_Trap_Monster));

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

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, tributeReleaseCount: 0);

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature?.CombatState == null)
            return;

        var monster = player.Creature.CombatState.CreateCard<The_First_Monarch_Trap_Monster>(player);
        monster.SetBattlePositionFromDuelCommand(attackPosition: true);

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, monster, choiceContext))
            return;

        _pendingLinkAfterZone = monster;
    }

    protected override Task OnAfterContinuousTrapEnteredSpellTrapZoneAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (_pendingLinkAfterZone != null)
        {
            YgoSpellTrapEquipLinkRegistry.Attach(this, _pendingLinkAfterZone);
            _pendingLinkAfterZone = null;
        }

        return Task.CompletedTask;
    }
}
