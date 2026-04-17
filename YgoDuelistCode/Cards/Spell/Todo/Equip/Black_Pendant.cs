using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;

public sealed class Black_Pendant : BaseEquipSpellCard, IYgoOnAddedToYgoGraveyardPile
{
    private const int PrintedAtkBonus = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkBonus) };

    public Black_Pendant()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Spell;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Black_Pendant),
    };

    public override bool CanEquipTo(BaseMonsterCard target) => true;

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped) =>
        new StatEffectTotal(DynamicVars["Mgc"].BaseValue, 0);

    public override bool CardShowsBlightKeyword => true;

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(3m);

    public async Task OnAddedToYgoGraveyardPileAsync(Player owner, CardPile pile)
    {
        var cs = owner.Creature?.CombatState;
        if (cs == null)
            return;

        int stacks = (int)DynamicVars["Mgc"].BaseValue;
        if (stacks <= 0)
            return;

        ulong mix = YgoDeterministicRng.MixSpellTrapZoneSlot(owner, this);
        string key = $"BLACK_PENDANT-{Id}-{pile.Cards.Count}";

        List<Creature> enemies = cs.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
            return;

        Creature? victim = YgoDeterministicRng.PickOne(cs, enemies, key, mix);
        if (victim == null || !victim.IsAlive)
            return;

        await PowerCmd.Apply<BlightPower>(victim, stacks, owner.Creature, this);
    }
}
