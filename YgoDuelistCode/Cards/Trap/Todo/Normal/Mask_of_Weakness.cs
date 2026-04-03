using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

/// <summary>
/// Normal Trap. Target a field monster; this card goes to the GY and stays linked like an equip.
/// While this card is in your GY and linked, that monster's ATK/DEF are multiplied by {Mgc}% (each copy stacks),
/// and it pays [E] 1 less for attack and defense commands.
/// </summary>
public sealed class Mask_of_Weakness : BaseTrapCard,
    IYgoSpellTrapEquipLink,
    IYgoSpellTrapEquipLinkStatEffect,
    IYgoPrePlayCancelableGridSelection
{
    private BaseMonsterCard? _equipLinkedMonster;
    private BaseMonsterCard? _pendingEquipLinkTarget;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 55m) };

    public Mask_of_Weakness()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[]
    {
        typeof(Narrow_Pass),
        typeof(Mask_of_Brutality),
        typeof(Mask_of_the_Burdened),
        typeof(Mask_of_Weakness),
    };

    public BaseMonsterCard? EquipLinkedMonster => _equipLinkedMonster;

    public void SetEquipLinkedMonster(BaseMonsterCard? monster) => _equipLinkedMonster = monster;

    bool IYgoSpellTrapEquipLink.DetachSpellTrapEquipLinkOnSpellTrapZoneToGraveyard => false;

    bool IYgoSpellTrapEquipLink.DestroyLinkedDuelMonsterOnSpellTrapZoneToGraveyard => false;

    public bool IsSpellTrapEquipLinkStatEffectActive =>
        Pile?.Type == GraveyardPile.CustomType && EquipLinkedMonster != null;

    public StatEffectTotalMultiplier GetSpellTrapEquipLinkStatMultiplier()
    {
        if (!IsSpellTrapEquipLinkStatEffectActive)
            return StatEffectTotalMultiplier.Identity;
        decimal p = DynamicVars["Mgc"].BaseValue * 0.01m;
        return new StatEffectTotalMultiplier(p, p);
    }

    public int GetSpellTrapEquipLinkAttackPlayEnergyDiscount() =>
        IsSpellTrapEquipLinkStatEffectActive ? 1 : 0;

    public int GetSpellTrapEquipLinkDefensePlayEnergyDiscount() =>
        IsSpellTrapEquipLinkStatEffectActive ? 1 : 0;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Count > 0;

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<BaseMonsterCard> field = DuelMonsterFieldRegistry.GetFieldMonsters(player).ToList();
        if (field.Count == 0)
            return false;

        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                field,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        var chosen = selected.FirstOrDefault() as BaseMonsterCard;
        if (chosen == null)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    protected override Task OnTrapPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return Task.CompletedTask;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseMonsterCard chosen)
            return Task.CompletedTask;

        if (!DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Contains(chosen))
            return Task.CompletedTask;

        _pendingEquipLinkTarget = chosen;
        return Task.CompletedTask;
    }

    protected override Task OnAfterNormalTrapSentToGraveyardAsync(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || _pendingEquipLinkTarget == null)
        {
            _pendingEquipLinkTarget = null;
            return Task.CompletedTask;
        }

        YgoSpellTrapEquipLinkRegistry.Attach(this, _pendingEquipLinkTarget);
        _pendingEquipLinkTarget = null;
        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(Owner);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(15m);
    }
}
