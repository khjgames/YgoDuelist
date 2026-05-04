using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Continuos;

public sealed class The_A_Forces : BaseContinuousSpellCard
{
    private const int PrintedAtkPerMonster = 2;
    private int _atkPerMonster = PrintedAtkPerMonster;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkPerMonster) };

    public The_A_Forces()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    public override StatEffectTotal GetContinuousStatEffect(BaseMonsterCard target)
    {
        if (Owner == null || target.Owner != Owner)
            return StatEffectTotal.None;

        int n = DuelMonsterFieldRegistry.OrderedFieldMonsters(Owner).Count;
        return new StatEffectTotal(_atkPerMonster * n, 0);
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        _atkPerMonster = PrintedAtkPerMonster + YgoStatUpgradeScaling.GetSpellTrapStatBonusUpgradeDelta(PrintedAtkPerMonster);
        DynamicVars["Mgc"].BaseValue = _atkPerMonster;
    }
}
