using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Secret_Pass_to_the_Treasures : BaseSpellCard, IYgoPlayCardActionPreSpendResourceFlow
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 10m), new DynamicVar("Mgc2", 50m) };

    public Secret_Pass_to_the_Treasures()
        : base(cost: 0, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<SecretPassTreasuresBlightPower>();
        }
    }

    /// <summary>Used by <see cref="IYgoPlayCardActionPreSpendResourceFlow"/> pre-play to filter field monsters.</summary>
    public decimal AtkThresholdForSelection => DynamicVars["Mgc"].BaseValue;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && BuildTargetCandidates(Owner, AtkThresholdForSelection).Count > 0;

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        if (!SecretPassPlayPayload.TryTakePending(this, out BaseMonsterCard? targetMonster) || targetMonster == null)
            return;

        Creature? pet = YgoMpCombatOrder.FirstPetWhere(Owner.PlayerCombatState, p =>
            p.IsAlive
            && DuelMonsterFieldRegistry.HasSourceCard(p, targetMonster));

        if (pet == null)
            return;

        decimal pct = DynamicVars["Mgc2"].BaseValue;
        await PowerCmd.Apply<SecretPassTreasuresBlightPower>(pet, pct, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].UpgradeValueBy(10m);
        DynamicVars["Mgc2"].UpgradeValueBy(50m);
    }

    public override bool TryGetPlayCardQueueOnActionEnqueuedDeferral(out string? reason)
    {
        reason = "secret_pass";
        return true;
    }

    async Task<bool> IYgoPlayCardActionPreSpendResourceFlow.TryPreparePreSpendPlayAsync(
        PlayCardAction action,
        Player player,
        CardModel self)
    {
        var spell = (Secret_Pass_to_the_Treasures)self;
        decimal maxAtk = spell.AtkThresholdForSelection;
        List<BaseMonsterCard> BuildCandidates() => BuildTargetCandidates(player, maxAtk);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1, 1)
        {
            RequireManualConfirmation = true,
            Cancelable = true
        };

        BaseMonsterCard? selected = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            player,
            prefs,
            BuildCandidates);
        if (selected == null)
            return false;

        SecretPassPlayPayload.SetPending(self, selected);
        return true;
    }

    void IYgoPlayCardActionPreSpendResourceFlow.ClearPreSpendPlayState(CardModel self) =>
        SecretPassPlayPayload.ClearForCard(self);

    private static List<BaseMonsterCard> BuildTargetCandidates(Player player, decimal maxAtk)
    {
        List<BaseMonsterCard> field = DuelMonsterFieldRegistry.OrderedFieldMonsters(player).ToList();
        return field
            .Where(m => m.CalcDuelMonsterStats(field).Atk <= maxAtk)
            .ToList();
    }
}
