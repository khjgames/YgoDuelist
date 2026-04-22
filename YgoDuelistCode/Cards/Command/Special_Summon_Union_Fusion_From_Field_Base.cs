using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Command card for extra-deck union fusions that are Special Summoned by banishing exact field materials.
/// </summary>
public abstract class Special_Summon_Union_Fusion_From_Field_Base : MonsterCommandCard, IYgoNHandPlayPhaseHighlightOverride
{
    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    protected override int CanonicalEnergyCost => 0;

    public override CardType Type => CardType.Skill;

    public override TargetType TargetType => TargetType.Self;

    protected abstract bool IsValidSourceMaterial(BaseMonsterCard source);

    protected abstract bool TryGetSummonData(Player player, out FusionMonsterCard fusionTarget, out List<BaseMonsterCard> materials);

    public Color? GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight)
    {
        _ = holder;
        if (Pile?.Type != YgoCardOptionPile.CustomType)
            return null;
        if (Owner == null)
            return null;
        try
        {
            if (!LocalContext.IsMe(Owner))
                return null;
        }
        catch
        {
            return null;
        }

        if (!vanillaWouldUseCyanPlayableHighlight)
            return null;

        return YgoNHandPlayPhaseHighlightColors.FusionStylePurple;
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || SourceMonster == null || SourceMonster.FaceDown || Owner == null)
                return false;
            if (SourceMonster is not BaseMonsterCard bm || !IsValidSourceMaterial(bm))
                return false;
            if (!TryGetSummonData(Owner, out _, out var materials) || materials.Count == 0)
                return false;
            if (!materials.Any(m => ReferenceEquals(m, bm)))
                return false;
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, materials.Count))
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, Owner);
            return pet != null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null || SourceMonster is not BaseMonsterCard source)
            return;
        Player player = Owner;
        if (!IsValidSourceMaterial(source))
            return;
        if (!TryGetSummonData(player, out FusionMonsterCard fusionTarget, out List<BaseMonsterCard> materials))
            return;
        if (!materials.Any(m => ReferenceEquals(m, source)))
            return;

        foreach (BaseMonsterCard m in materials)
        {
            Creature? pet = TributeSummonSelection.ResolvePetForFieldCard(player, m);
            if (pet == null || !pet.IsAlive)
                return;
            await CreatureCmd.Kill(pet, force: true);
        }

        foreach (BaseMonsterCard m in materials)
            await YgoBanishedService.BanishCard(player, m);

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, fusionTarget, choiceContext))
            return;

        Creature? summonedPet = TributeSummonSelection.ResolvePetForFieldCard(player, fusionTarget);
        if (summonedPet == null || !summonedPet.IsAlive || player.Creature == null)
            return;

        if (!YgoStumblingField.IsActive(player))
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(summonedPet, true, player.Creature, fusionTarget);
    }
}
