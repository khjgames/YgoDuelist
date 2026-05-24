using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Field;

/// <summary>
/// Field Spell: remains face-up in the field zone. Right-click during your turn to Fusion Summon
/// (materials banished). Purple highlight while at least one legal fusion is possible.
/// </summary>
public sealed class Fusion_Gate : BaseFieldSpellCard, IFusionSpellSource, IYgoCardZoneRightClick
{
    private bool _fusionGateFlowActive;

    public Fusion_Gate()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public Type FusionTargetMonsterType => typeof(FusionMonsterCard);

    public bool BanishesFusionMaterials => true;

    public bool RequiresPlayerFusionTargetSelection => false;

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fusion | YgoCardPackTags.Spell | YgoCardPackTags.Banish;

    /// <summary>
    /// Multiplier for YGO pack reward weighted picks of this specific card (within its own rarity)(<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// Applied to base weight before trunk copies, related bonus, and duplicate-in-pack damping. Default <c>1</c>.
    /// </summary>
    public override float PackWeightMultiplier => 1.8f;

    /// <summary>
    /// How likely it is (within its own rarity), to get duplicates of this card after the first (<see cref="YgoDuelist.YgoDuelistCode.Services.YgoCardPackGenerator"/>).
    /// </summary>
    public override float DuplicateFatigue => 0.64f;

    public override Type[] RelatedCards => GetRelatedCards(typeof(Polymerization));

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    public YgoCardRightClickActivation RightClickActivationMask =>
        YgoCardRightClickActivation.SpellTrapZoneFaceUp;

    public bool TryHandleCardZoneRightClick(NHandCardHolder holder) =>
        TryHandleZoneRightClick(holder, this);

    public override Color? GetNHandPlayPhaseHighlightModulateOverride(
        NHandCardHolder holder,
        bool vanillaWouldUseCyanPlayableHighlight)
    {
        _ = holder;
        PileType pileType = Pile?.Type ?? PileType.None;
        if (pileType != SpellTrapZonePile.CustomType && pileType != PileType.Hand)
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

        if (!FusionSummonSelection.HasFeasibleFusionPlay(Owner, this))
            return null;

        bool faceUpInZone = pileType == SpellTrapZonePile.CustomType && !FaceDown;
        if (!vanillaWouldUseCyanPlayableHighlight && !faceUpInZone)
            return null;

        return YgoNHandPlayPhaseHighlightColors.FusionStylePurple;
    }

    /// <summary>Right-click on this card in the Spell/Trap zone (second hand). Returns true if the click was consumed.</summary>
    public static bool TryHandleZoneRightClick(NHandCardHolder holder, Fusion_Gate card)
    {
        if (!IsLocalControllingOwner(card))
            return false;

        if (card._fusionGateFlowActive)
            return true;

        if (card.Owner == null
            || !FusionSummonSelection.HasFeasibleFusionPlay(card.Owner, card))
            return false;

        _ = RunFusionGateFusionAsync(holder, card);
        return true;
    }

    private static bool IsLocalControllingOwner(CardModel card)
    {
        if (card.Owner?.RunState == null)
            return false;
        Player? me = LocalContext.GetMe(card.Owner.RunState);
        return me != null && ReferenceEquals(me, card.Owner);
    }

    private static async Task RunFusionGateFusionAsync(NHandCardHolder holder, Fusion_Gate gate)
    {
        gate._fusionGateFlowActive = true;
        try
        {
            Player? player = gate.Owner;
            if (player == null)
                return;

            if (!await FusionSummonSelection.TrySelectFusionResolutionAsync(player, gate))
                return;

            if (!FusionSpellPlayPayload.TryTakePendingForCard(gate, out FusionSpellPendingResolution? pending) || pending == null)
                return;

            var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
            await FusionSummonSelection.ApplyResolvedFusionAsync(player, gate, pending, ctx);
        }
        finally
        {
            gate._fusionGateFlowActive = false;
            SpellTrapCardRightClickPatch.RefreshHolder(holder);
        }
    }
}
