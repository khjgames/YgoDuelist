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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Fusion;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Command;

/// <summary>
/// Extra command while <see cref="Egyptian_God_Slime"/> is in the Extra Deck: tribute this Level 10 Aqua 0-ATK monster to Special Summon it.
/// </summary>
public sealed class Special_Summon_Egyptian_God_Slime : MonsterCommandCard, IYgoNHandPlayPhaseHighlightOverride
{
    protected override bool MirrorSourceMonsterUpgradeVisual => true;

    protected internal override string? CommandEnergyIconPrefix => "silent";

    public Special_Summon_Egyptian_God_Slime()
    {
    }

    protected override int CanonicalEnergyCost => 0;

    public override CardType Type => CardType.Skill;

    public override TargetType TargetType => TargetType.Self;

    public Color? GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight)
    {
        _ = holder;
        PileType pileType = Pile?.Type ?? PileType.None;
        if (pileType != YgoCardOptionPile.CustomType)
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
            if (!base.IsPlayable || SourceMonster == null || SourceMonster.FaceDown)
                return false;
            if (SourceMonster is not BaseMonsterCard bm || !Egyptian_God_Slime.QualifiesAsSlimeTributeMaterial(bm))
                return false;
            Player? p = Owner;
            if (p == null || !Egyptian_God_Slime.PlayerHasSlimeInExtraDeck(p))
                return false;
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(p, 1))
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(SourceMonster, p);
            return pet != null;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.PlayerCombatState == null || SourceMonster is not NormalMonsterCard source)
            return;
        Player player = Owner;

        if (source is not BaseMonsterCard bm || !Egyptian_God_Slime.QualifiesAsSlimeTributeMaterial(bm))
            return;

        CardPile? extra = ExtraDeckPile.CustomType.GetPile(player);
        Egyptian_God_Slime? slime = extra?.Cards.OfType<Egyptian_God_Slime>().FirstOrDefault();
        if (slime == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        await CreatureCmd.Kill(pet, force: true);

        CardPile? grave = GraveyardPile.CustomType.GetPile(player);
        if (grave != null)
            await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, slime, choiceContext))
            return;

        Creature? slimePet = TributeSummonSelection.ResolvePetForFieldCard(player, slime);
        if (slimePet == null || !slimePet.IsAlive || player.Creature == null)
            return;

        if (!YgoStumblingField.IsActive(player))
            await MonsterCommandRegistry.SetHasUsedCommandThisTurn(slimePet, true, player.Creature, slime);
    }
}
