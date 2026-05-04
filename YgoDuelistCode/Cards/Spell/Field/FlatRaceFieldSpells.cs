using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Field;

/// <summary>+2/+2 for listed races; upgraded uses <see cref="YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade"/>.</summary>
public abstract class FlatSymmetricRaceFieldSpell : BaseFieldSpellCard
{
    private const int PrintedAtkDef = 2;
    private readonly YgoCardPackTags _packTags;

    protected FlatSymmetricRaceFieldSpell(YgoCardPackTags packTags)
        : base(1, CardRarity.Common, TargetType.Self)
    {
        _packTags = YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | packTags;
    }

    public sealed override YgoCardPackTags PackTags => _packTags;

    protected abstract bool IsBuffedRace(DuelMonsterRace r);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedAtkDef) };

    public sealed override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target) =>
        IsBuffedRace(target.DuelMonsterRace)
            ? new StatEffectTotal(
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded),
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, IsUpgraded))
            : StatEffectTotal.None;

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedAtkDef, true);
    }
}

public sealed class Forest : FlatSymmetricRaceFieldSpell
{
    public Forest() : base(YgoCardPackTags.Earth) { }

    protected override bool IsBuffedRace(DuelMonsterRace r) =>
        r is DuelMonsterRace.Insect or DuelMonsterRace.Beast or DuelMonsterRace.Plant or DuelMonsterRace.BeastWarrior;
}

public sealed class Mountain : FlatSymmetricRaceFieldSpell
{
    public Mountain() : base(YgoCardPackTags.Earth) { }

    protected override bool IsBuffedRace(DuelMonsterRace r) =>
        r is DuelMonsterRace.Dragon or DuelMonsterRace.WingedBeast or DuelMonsterRace.Thunder;
}

public sealed class Sogen : FlatSymmetricRaceFieldSpell
{
    public Sogen() : base(YgoCardPackTags.Earth) { }

    protected override bool IsBuffedRace(DuelMonsterRace r) =>
        r is DuelMonsterRace.Warrior or DuelMonsterRace.BeastWarrior;
}

public sealed class Wasteland : FlatSymmetricRaceFieldSpell
{
    public Wasteland() : base(YgoCardPackTags.Earth) { }

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.ZombieBoost;

    protected override bool IsBuffedRace(DuelMonsterRace r) =>
        r is DuelMonsterRace.Dinosaur or DuelMonsterRace.Zombie or DuelMonsterRace.Rock;
}

/// <summary>+2/+2 buff races and −2/−2 debuff races; upgraded uses spell/trap scaling.</summary>
public abstract class FlatBuffDebuffRaceFieldSpell : BaseFieldSpellCard
{
    private const int PrintedBuffMag = 2;
    private const int PrintedDebuffMag = -2;
    private readonly YgoCardPackTags _packTags;

    protected FlatBuffDebuffRaceFieldSpell(YgoCardPackTags packTags)
        : base(1, CardRarity.Common, TargetType.Self)
    {
        _packTags = YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell | packTags;
    }

    public sealed override YgoCardPackTags PackTags => _packTags;

    protected abstract bool IsBuffRace(DuelMonsterRace r);
    protected abstract bool IsDebuffRace(DuelMonsterRace r);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", (decimal)PrintedBuffMag) };

    public sealed override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target)
    {
        DuelMonsterRace r = target.DuelMonsterRace;
        if (IsBuffRace(r))
            return new StatEffectTotal(
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedBuffMag, IsUpgraded),
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedBuffMag, IsUpgraded));
        if (IsDebuffRace(r))
            return new StatEffectTotal(
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedDebuffMag, IsUpgraded),
                YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedDebuffMag, IsUpgraded));
        return StatEffectTotal.None;
    }

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].BaseValue = System.Math.Abs(
            YgoStatUpgradeScaling.ApplySpellTrapStatBonusUpgrade(PrintedBuffMag, true));
    }
}

public sealed class Umi : FlatBuffDebuffRaceFieldSpell
{
    public Umi() : base(YgoCardPackTags.Water | YgoCardPackTags.Ocean) { }

    protected override bool IsBuffRace(DuelMonsterRace r) =>
        r is DuelMonsterRace.Fish or DuelMonsterRace.SeaSerpent or DuelMonsterRace.Thunder or DuelMonsterRace.Aqua;

    protected override bool IsDebuffRace(DuelMonsterRace r) =>
        r is DuelMonsterRace.Machine or DuelMonsterRace.Pyro;
}

public sealed class Yami : FlatBuffDebuffRaceFieldSpell
{
    public Yami() : base(YgoCardPackTags.Dark) { }

    protected override bool IsBuffRace(DuelMonsterRace r) =>
        r is DuelMonsterRace.Fiend or DuelMonsterRace.Spellcaster;

    protected override bool IsDebuffRace(DuelMonsterRace r) => r == DuelMonsterRace.Fairy;
}
