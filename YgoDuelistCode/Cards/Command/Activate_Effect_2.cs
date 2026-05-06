using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Second optional field effect on monsters implementing <see cref="IMonsterSecondActivatedEffect"/>.
/// </summary>
public sealed class Activate_Effect_2 : MonsterCommandCard, IActivateEffectPileUi
{
    private IMonsterSecondActivatedEffect? Effect => SourceMonster as IMonsterSecondActivatedEffect;

    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    public Activate_Effect_2()
    {
    }

    protected override int CanonicalEnergyCost
    {
        get
        {
            if (SourceMonster is not IMonsterSecondActivatedEffect impl)
                return 0;
            var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            if (pet != null && MonsterCommandRegistry.GetOrCreate(pet).ZeroEnergyMonsterCommandsThisTurn)
                return 0;
            return impl.SecondActivatedEffectEnergyCost;
        }
    }

    public override CardType Type => Effect?.SecondActivatedEffectCardType ?? CardType.Skill;

    public override TargetType TargetType => Effect?.SecondActivatedEffectTarget ?? TargetType.Self;

    public override string PortraitPath
    {
        get
        {
            TryResolveSourceMonsterFromStoredPetId();
            if (Effect?.SecondActivatedEffectPortraitPath is { Length: > 0 } path)
                return path;
            return base.PortraitPath;
        }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster is not IMonsterSecondActivatedEffect impl)
                return false;
            if (SourceMonster.FaceDown && !SourceMonster.AllowsActivateEffectWhileFaceDownFlipFaceUp)
                return false;
            if (!impl.IsSecondActivatedEffectAvailable)
                return false;
            var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            if (pet == null)
                return false;
            bool regularDeckBypass = IsRegularDeckMonsterCommandWithLivePet(pet);
            if (impl.SecondActivatedEffectConsumesOncePerTurnSlot && !regularDeckBypass)
            {
                MonsterCommandState reg = MonsterCommandRegistry.GetOrCreate(pet);
                if (reg.HasUsedSecondActivatedEffectThisTurn)
                    return false;
            }

            return true;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || SourceMonster is not IMonsterSecondActivatedEffect impl)
            return;

        if (SourceMonster.FaceDown)
        {
            if (!SourceMonster.AllowsActivateEffectWhileFaceDownFlipFaceUp
                || SourceMonster is not AbstractMonsterCard amc)
                return;

            if (!FlipFaceDownOnPlayerEnemyAttackHelpers.ForceFlipFaceUpWithoutActivatingEffectNow(amc, choiceContext))
                return;

            await DuelMonsterStancePowerSync.SyncSummonedPetIfPresentAsync(amc, Owner.Creature);
        }

        var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
        if (pet != null)
            await YgoNarrowPassField.ApplyMonsterCommandLifePaymentIfActiveAsync(choiceContext, Owner, pet);

        await impl.OnSecondActivatedEffect(choiceContext, cardPlay, SourceMonster);
    }

    internal bool TryGetPileDescriptionForActivateEffect2(ref string result)
    {
        if (SourceMonster is not NormalMonsterCard src2 || src2 is not IMonsterSecondActivatedEffect impl2)
            return false;
        var loc2 = new LocString("cards", impl2.SecondActivatedEffectDescriptionLocKey);
        src2.DynamicVars.AddTo(loc2);
        string text2 = loc2.GetFormattedText();
        if (string.IsNullOrEmpty(text2))
            return false;
        result = text2;
        return true;
    }

    internal bool TryGetTitleForActivateEffect2(ref string result)
    {
        var loc2 = new LocString("cards", "YGODUELIST-ACTIVATE_EFFECT_2.title");
        string t2 = loc2.GetFormattedText();
        if (string.IsNullOrEmpty(t2))
            return false;
        result = t2;
        return true;
    }

    bool IActivateEffectPileUi.TryGetActivateEffectPileDescription(ref string result) =>
        TryGetPileDescriptionForActivateEffect2(ref result);

    bool IActivateEffectPileUi.TryGetActivateEffectPileTitle(ref string result) =>
        TryGetTitleForActivateEffect2(ref result);
}
