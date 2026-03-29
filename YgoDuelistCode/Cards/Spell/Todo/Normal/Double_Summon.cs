using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Double_Summon : BaseSpellCard
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    public Double_Summon()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Spell | YgoCardPackTags.Dark | YgoCardPackTags.Spellcaster | YgoCardPackTags.Normal;

    public override Type[] RelatedCards =>
    [
        typeof(Double_Summon),
        typeof(Legion_the_Fiend_Jester),
        typeof(Dark_Magician),
        typeof(Dark_Magician_Girl),
        typeof(Skilled_Dark_Magician),
        typeof(Dark_Magician_of_Chaos),
        typeof(Toon_Dark_Magician_Girl),
        typeof(Dark_Magic_Attack),
    ];

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;
        await PlayerCmd.GainStars(1, Owner);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("conduitIcon", ConduitImgBbcode);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
