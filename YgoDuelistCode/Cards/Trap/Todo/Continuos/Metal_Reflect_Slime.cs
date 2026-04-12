using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.TrapMonster;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;

public sealed class Metal_Reflect_Slime : BaseContinuousTrapCard, IYgoSpellTrapEquipLink
{
    private BaseMonsterCard? _equipLinkedMonster;
    private uint _equipLinkedPetCombatId;
    private BaseMonsterCard? _pendingLinkAfterZone;

    public Metal_Reflect_Slime()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.God;

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

        var monster = player.Creature.CombatState.CreateCard<Metal_Reflect_Slime_Trap_Monster>(player);
        monster.SetBattlePositionFromDuelCommand(attackPosition: false);

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
