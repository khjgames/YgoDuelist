using BaseLib.Abstracts;
using Godot;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Extensions;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Powers;

/// <summary>Temporary Strength loss from <see cref="Slifer_the_Sky_Dragon"/> (Slifer's Pressure).</summary>
public sealed class SlifersPressureTemporaryStrengthPower : TemporaryStrengthPower, ICustomPower
{
    private AbstractModel? _origin;
    private Slifer_the_Sky_Dragon? _sliferSource;
    private bool _hasAppliedBlight;
    private bool _checkingBlight;

    private const int DefaultBlightAmount = 10;

    public override bool IsInstanced => true;

    public override AbstractModel OriginModel => _origin ?? ModelDb.Card<Slifer_the_Sky_Dragon>();

    protected override bool IsPositive => false;

    public override LocString Title => new("powers", "YGODUELIST-SLIFERS_PRESSURE_POWER.title");

    public override LocString Description => new("powers", "YGODUELIST-SLIFERS_PRESSURE_POWER.description");

    protected override string SmartDescriptionLocKey => "YGODUELIST-SLIFERS_PRESSURE_POWER.smartDescription";

    string? ICustomPower.CustomPackedIconPath => "slifer_the_sky_dragon.png".CardImagePath();

    string? ICustomPower.CustomBigIconPath => "slifer_the_sky_dragon.png".CardImagePath();

    string? ICustomPower.CustomBigBetaIconPath => null;

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _origin = cardSource ?? ModelDb.Card<Slifer_the_Sky_Dragon>();
        _sliferSource = cardSource as Slifer_the_Sky_Dragon;
        await base.BeforeApplied(target, amount, applier, cardSource);
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        await TryApplyBlightIfAttackIntentIsZeroAsync("AfterApplied");
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(power, amount, applier, cardSource);
        if (power is BlightPower)
            return;
        if (!ReferenceEquals(power.Owner, Owner))
            return;

        await TryApplyBlightIfAttackIntentIsZeroAsync($"AfterPowerAmountChanged:{power.Id.Entry}");
    }

    private async Task TryApplyBlightIfAttackIntentIsZeroAsync(string sourceTag)
    {
        if (_hasAppliedBlight || _checkingBlight)
            return;

        if (!Owner.IsAlive || Owner.Side != MegaCrit.Sts2.Core.Combat.CombatSide.Enemy)
            return;

        if (!YgoIntentAttackDamage.HasAttackIntent(Owner))
            return;

        Creature? controllerCreature = _sliferSource?.Owner?.Creature ?? Applier;
        if (controllerCreature == null)
        {
            GD.Print($"[YgoDuelist][SliferPressure] skip {sourceTag}: missing controller enemyCombatId={Owner.CombatId}");
            return;
        }

        _checkingBlight = true;
        try
        {
            int intentDamage = YgoIntentAttackDamage.GetTotalAttackIntentDamage(Owner, controllerCreature);
            int blight = GetBlightAmount();
            GD.Print($"[YgoDuelist][SliferPressure] check {sourceTag}: enemyCombatId={Owner.CombatId} intentDamage={intentDamage} blight={blight} pressure={Amount}");

            if (intentDamage > 0 || blight <= 0)
                return;

            _hasAppliedBlight = true;
            await PowerCmd.Apply<BlightPower>(Owner, blight, controllerCreature, _sliferSource);
            GD.Print($"[YgoDuelist][SliferPressure] applied Blight enemyCombatId={Owner.CombatId} amount={blight} source={sourceTag}");
        }
        finally
        {
            _checkingBlight = false;
        }
    }

    private int GetBlightAmount()
    {
        if (_sliferSource?.DynamicVars != null && _sliferSource.DynamicVars.ContainsKey("Mgc"))
            return (int)_sliferSource.DynamicVars["Mgc"].BaseValue;
        return DefaultBlightAmount;
    }
}
