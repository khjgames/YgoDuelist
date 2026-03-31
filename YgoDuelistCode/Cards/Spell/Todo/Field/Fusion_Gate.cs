using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Patches;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Field;

/// <summary>
/// Field Spell: remains face-up in the field zone. Right-click during your turn to Fusion Summon
/// (materials banished). Purple highlight while at least one legal fusion is possible.
/// </summary>
public sealed class Fusion_Gate : BaseFieldSpellCard, IFusionSpellSource
{
    private bool _fusionGateFlowActive;

    public Fusion_Gate()
        : base(cost: 1, rarity: CardRarity.Uncommon, target: TargetType.Self)
    {
    }

    public Type FusionTargetMonsterType => typeof(FusionMonsterCard);

    public bool BanishesFusionMaterials => true;

    public bool RequiresPlayerFusionTargetSelection => false;

    public override YgoCardPackTags PackTags => YgoCardPackTags.Fusion | YgoCardPackTags.Spell | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Fusion_Gate), typeof(Polymerization) };

    public override StatEffectTotal GetFieldStatEffect(BaseMonsterCard target) => StatEffectTotal.None;

    protected override Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override void OnUpgrade()
    {
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

            if (!FusionSpellPlayPayload.TryTakePending(gate, out FusionSpellPendingResolution? pending) || pending == null)
                return;

            var ctx = new BlockingPlayerChoiceContext();
            await FusionSummonSelection.ApplyResolvedFusionAsync(player, gate, pending, ctx);
        }
        finally
        {
            gate._fusionGateFlowActive = false;
            SpellTrapCardRightClickPatch.RefreshHolder(holder);
        }
    }
}
