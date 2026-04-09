using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Chain_Summoning : BaseSpellCard
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    public override bool UseAlternateUpgradedDescription => true;

    public Chain_Summoning()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        decimal every = IsUpgraded ? 3m : 4m;
        await PowerCmd.Apply<ChainSummoningPower>(Owner.Creature, every, Owner.Creature, this);
    }

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);
}
