using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Single monster-options command; behavior and display come from <see cref="IMonsterActivatedEffect"/> on <see cref="MonsterCommandCard.SourceMonster"/>.
/// </summary>
public sealed class Activate_Effect : MonsterCommandCard
{
    private IMonsterActivatedEffect? Effect => SourceMonster as IMonsterActivatedEffect;

    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    internal override bool LogsOptionPileLifecycle => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    public Activate_Effect()
    {
    }

    public override int CanonicalStarCost =>
        SourceMonster is Gravekeeper_s_Chief ? 1 : base.CanonicalStarCost;

    public override int CurrentStarCost =>
        SourceMonster is Gravekeeper_s_Chief ? 1 : base.CurrentStarCost;

    protected override int CanonicalEnergyCost
    {
        get
        {
            if (SourceMonster is not IMonsterActivatedEffect impl)
                return 0;
            var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            if (pet != null && MonsterCommandRegistry.GetOrCreate(pet).ZeroEnergyMonsterCommandsThisTurn)
                return 0;
            return impl.ActivatedEffectEnergyCost;
        }
    }

    public override CardType Type => Effect?.ActivatedEffectCardType ?? CardType.Skill;

    public override TargetType TargetType => Effect?.ActivatedEffectTarget ?? TargetType.Self;

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster is not IMonsterActivatedEffect impl)
                return false;
            if (SourceMonster.FaceDown)
                return false;
            if (!SourceMonster.IsActivatedEffectAvailableInCommandContext(Owner))
                return false;
            var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            if (pet == null)
                return false;
            bool regularDeckBypass = IsRegularDeckMonsterCommandWithLivePet(pet);
            if (impl.ActivatedEffectConsumesOncePerTurnSlot && !regularDeckBypass)
            {
                MonsterCommandState reg = MonsterCommandRegistry.GetOrCreate(pet);
                if (reg.HasUsedActivatedEffectThisTurn)
                    return false;
            }
            return true;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || SourceMonster is not IMonsterActivatedEffect impl || SourceMonster.FaceDown)
            return;

        var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
        if (pet != null)
            await YgoNarrowPassField.ApplyMonsterCommandLifePaymentIfActiveAsync(choiceContext, Owner, pet);

        await impl.OnActivatedEffect(choiceContext, cardPlay, SourceMonster);
    }

    internal bool TryGetPileDescriptionForActivateEffect(ref string result)
    {
        if (SourceMonster is not NormalMonsterCard sourceMonster || sourceMonster is not IMonsterActivatedEffect impl)
            return false;
        var loc = new LocString("cards", impl.ActivatedEffectDescriptionLocKey);
        sourceMonster.DynamicVars.AddTo(loc);
        string text = loc.GetFormattedText();
        if (string.IsNullOrEmpty(text))
            return false;
        result = text;
        return true;
    }

    internal bool TryGetTitleForActivateEffect(ref string result)
    {
        var loc = new LocString("cards", "YGODUELIST-ACTIVATE_EFFECT.title");
        string text = loc.GetFormattedText();
        if (string.IsNullOrEmpty(text))
            return false;
        result = text;
        return true;
    }
}
