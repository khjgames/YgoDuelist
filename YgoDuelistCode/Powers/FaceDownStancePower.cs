using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

public sealed class FaceDownStancePower : YgoDuelistInfoPower
{
    public override LocString Title => new("powers", "FACE_DOWN_STANCE_POWER.title");

    public override LocString Description => new("powers", "FACE_DOWN_STANCE_POWER.description");

    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return Task.CompletedTask;
        if (dealer == null || dealer.Side != CombatSide.Enemy)
            return Task.CompletedTask;
        if (!props.HasFlag(ValueProp.Move) || !props.IsPoweredAttack())
            return Task.CompletedTask;
        if (DuelMonsterFieldRegistry.GetSourceMonster<Spear_Cretin>(target) is not { FaceDown: true } spear)
            return Task.CompletedTask;
        if (!MonsterCommandRegistry.PetHasUsedAnyCommandSlotThisTurn(target))
            return Task.CompletedTask;

        bool wasFaceDown = spear.FaceDown;
        spear.FaceDown = false;
        spear.UpdateFaceDownKeywordFromBool();
        bool marked = YgoMonsterFlipEffectRunner.MarkFlippedFaceUpOnField(spear, wasFaceDown);
        GD.Print(
            $"[YgoDuelist][MP][FaceDown] Spear Cretin flips before battle damage source={spear.Id?.Entry} wasFaceDown={wasFaceDown} marked={marked}");
        return Task.CompletedTask;
    }
}
