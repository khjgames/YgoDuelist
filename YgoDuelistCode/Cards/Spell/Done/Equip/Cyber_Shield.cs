using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Equip;

public sealed class Cyber_Shield : BaseEquipSpellCard
{
    private const int PrintedBonus = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedBonus) };

    public Cyber_Shield()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | YgoCardPackTags.Wind;

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.HarpieLady;

    protected override System.Type[] PreviewReferencedCardTypes => new[] { typeof(Harpie_Lady) };

    public override bool CanEquipTo(BaseMonsterCard target) =>
        YgoMonsterArchetypeKeywords.HarpieLadyArchetypeMonsterTypes.Contains(target.GetType());

    public override StatEffectTotal GetEquipStatEffect(BaseMonsterCard equipped)
    {
        int bonus = (int)DynamicVars["Mgc"].BaseValue;
        return new StatEffectTotal(bonus, bonus);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].BaseValue = 8m;
}
