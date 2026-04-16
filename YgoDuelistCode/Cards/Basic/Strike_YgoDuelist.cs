using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace YgoDuelist.YgoDuelistCode.Cards.Basic;

public sealed class Strike_YgoDuelist : YgoDuelistCard
{
    public override string? YgoStrikeDefendEnergyIconTexturePath => "YgoDuelist/images/card_frames/attack_monster_energy_icon.png";

    public override (float H, float S, float V)? CustomFrameTintHsv => (0f, 0f, 0.6f);

    public override string CustomPortraitPath => VanillaBorrowedPortraitPaths.PackedPng<StrikeSilent>();

    public override string PortraitPath => ModelDb.Card<StrikeSilent>().PortraitPath;

    public override string BetaPortraitPath => ModelDb.Card<StrikeSilent>().BetaPortraitPath;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DamageVar(6m, ValueProp.Move) };

    public Strike_YgoDuelist()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
