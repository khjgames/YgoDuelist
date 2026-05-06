using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Spend 1 stack of <see cref="ConsumableShacklesPower"/> on this monster to apply <see cref="ActiveShacklesPower"/> to a targeted enemy.
/// </summary>
public sealed class Activate_Shackles : MonsterCommandCard
{
    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    public override LocString? GetPatchedDescriptionLocStringForDisplay()
    {
        var loc = new LocString("cards", Id.Entry + ".description");
        DynamicVars.AddTo(loc);
        DynamicVars["ShacklesStr"].BaseValue = ActiveShacklesPower.StrengthLossPerApply;
        return loc;
    }

    protected override int CanonicalEnergyCost => 0;

    public override CardType Type => CardType.Skill;

    public override TargetType TargetType => TargetType.AnyEnemy;

    public override string PortraitPath => ImageHelper.GetImagePath("packed/card_portraits/colorless/dark_shackles.png");

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<ActiveShacklesPower>();
        }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster == null || SourceMonster.FaceDown)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            if (pet == null)
                return false;
            return pet.GetPower<ConsumableShacklesPower>() is ConsumableShacklesPower cs && cs.Amount >= 1m;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        Creature? target = cardPlay.Target;
        if (player?.PlayerCombatState == null || SourceMonster == null || target == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, player);
        ConsumableShacklesPower? cons = pet?.GetPower<ConsumableShacklesPower>();
        if (pet == null || cons == null || cons.Amount < 1m)
            return;

        await YgoNarrowPassField.ApplyMonsterCommandLifePaymentIfActiveAsync(choiceContext, player, pet);

        await PowerCmd.Apply<ActiveShacklesPower>(target, ActiveShacklesPower.StrengthLossPerApply, player.Creature, this);
        await PowerCmd.ModifyAmount(cons, -1m, player.Creature, this);
        if (cons.Amount <= 0m)
            await PowerCmd.Remove(cons);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }
}
