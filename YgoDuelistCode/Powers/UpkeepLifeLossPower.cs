using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Field pet: at the start of the controlling player's turn, lose HP equal to stacks; <see cref="Cure_Mermaid"/> also heals the player 1 after that loss.</summary>
public sealed class UpkeepLifeLossPower : YgoDuelistPower
{
    private CardModel? _sourceCard;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => new("powers", "YGODUELIST-UPKEEP_LIFE_LOSS_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-UPKEEP_LIFE_LOSS_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-UPKEEP_LIFE_LOSS_POWER.smartDescription";

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _sourceCard = cardSource;
        await base.AfterApplied(applier, cardSource);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(Owner) is { FaceDown: true })
            return;

        if (Owner.PetOwner != player)
            return;

        int loss = (int)Amount;
        if (loss <= 0)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            loss,
            ValueProp.Unblockable | ValueProp.Unpowered,
            player.Creature,
            _sourceCard);

        if (_sourceCard is Cure_Mermaid && player.Creature != null)
            await CreatureCmd.Heal(player.Creature, 1m);
    }
}
