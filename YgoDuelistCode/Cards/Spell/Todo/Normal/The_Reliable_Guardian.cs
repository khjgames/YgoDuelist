using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class The_Reliable_Guardian : BaseSpellCard
{
    public The_Reliable_Guardian()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(The_Reliable_Guardian),
    };

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Any();

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null)
            return;

        if (!RushReliablePlayPayload.TryTakePending(this, out var targetMonster) || targetMonster == null)
            return;

        Creature? targetPet = Owner.PlayerCombatState.Pets
            .FirstOrDefault(p => p.IsAlive && ReferenceEquals(DuelMonsterFieldRegistry.GetSourceCardForPet(p), targetMonster));
        if (targetPet == null)
            return;

        await PowerCmd.Apply<ReliableDefenderPower>(targetPet, 1m, Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
