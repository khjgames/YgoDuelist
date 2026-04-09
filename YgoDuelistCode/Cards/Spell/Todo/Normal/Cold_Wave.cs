using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Cold_Wave : BaseSpellCard
{
    private const decimal BlockPerX = 8m;

    protected override int CanonicalEnergyCost => 0;

    protected override bool HasEnergyCostX => true;

    public Cold_Wave()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Water | YgoCardPackTags.Ocean | YgoCardPackTags.Spell;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        int x = ResolveEnergyXValue();
        decimal total = BlockPerX * x;
        if (total > 0m)
            await CreatureCmd.GainBlock(Owner.Creature, total, default, cardPlay);

        await PowerCmd.Apply<ColdWaveSpellTrapLockPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}
