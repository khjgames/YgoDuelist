using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Continuos;

public sealed class Talisman_of_Spell_Sealing : BaseContinuousTrapCard, IYgoAfterDuelMonsterDiedZoneCard, IYgoSealmasterDependentTalisman
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 2m) };

    public Talisman_of_Spell_Sealing()
        : base(cost: 0, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<TalismanSpellSealingFieldPower>();
        }
    }

    public Task AfterDuelMonsterDiedAsync(DuelMonsterPetDeathContext ctx) =>
        YgoSealmasterMeiseiGate.DestroyTalismansIfNoSealmaster(ctx.Player);

    protected override bool IsPlayable =>
        base.IsPlayable
        && YgoSealmasterMeiseiGate.HasFaceUpSealmaster(Owner);

    protected override async Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature != null)
            await PowerCmd.Apply<TalismanSpellSealingFieldPower>(Owner.Creature, DynamicVars["Mgc"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Mgc"].UpgradeValueBy(1m);
}
