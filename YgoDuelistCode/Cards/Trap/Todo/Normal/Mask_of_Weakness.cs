using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Equip;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Continuos;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Trap.Todo.Normal;

/// <summary>
/// Normal Trap: cancelable pick a field monster, pay cost, then remain face-up in the Spell/Trap zone linked to that monster
/// (equip-style hover overlay). While active, applies ATK/DEF multiplier and −1 attack/defense command energy. Destroyed when the
/// monster leaves the field; unlinking does not destroy the monster when this trap is sent to the GY.
/// </summary>
public sealed class Mask_of_Weakness : BaseTrapCard,
    IYgoSpellTrapEquipLink,
    IYgoSpellTrapEquipLinkStatEffect,
    IYgoPrePlayCancelableGridSelection
{
    private BaseMonsterCard? _equipLinkedMonster;
    private uint _equipLinkedPetCombatId;
    private BaseMonsterCard? _pendingEquipLinkTarget;
    private bool _fizzleToGraveyard;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", 55m) };

    public Mask_of_Weakness()
        : base(cost: 1, rarity: CardRarity.Rare, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.TrapNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[]
    {
        typeof(Narrow_Pass),
        typeof(Mask_of_Brutality),
        typeof(Mask_of_the_Burdened),
        typeof(Mask_of_Weakness),
    };

    protected override bool SendsTrapToGraveyardAfterPlay => false;

    public BaseMonsterCard? EquipLinkedMonster
    {
        get
        {
            YgoSpellTrapEquipLinkRegistry.TryRebindEquipLinkIfNeeded(this);
            return _equipLinkedMonster;
        }
    }

    public uint EquipLinkedPetCombatId => _equipLinkedPetCombatId;

    public void SetEquipLinkedMonster(BaseMonsterCard? monster) => _equipLinkedMonster = monster;

    public void SetEquipLinkedPetCombatId(uint petCombatId) => _equipLinkedPetCombatId = petCombatId;

    bool IYgoSpellTrapEquipLink.DetachSpellTrapEquipLinkOnSpellTrapZoneToGraveyard => true;

    bool IYgoSpellTrapEquipLink.DestroyLinkedDuelMonsterOnSpellTrapZoneToGraveyard => false;

    public bool IsSpellTrapEquipLinkStatEffectActive =>
        Pile?.Type == SpellTrapZonePile.CustomType
        && !FaceDown
        && EquipLinkedMonster != null;

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

    protected override bool IsPlayable
    {
        get
        {
            if (Pile?.Type == SpellTrapZonePile.CustomType && !FaceDown && EquipLinkedMonster != null)
                return false;

            if (!base.IsPlayable)
                return false;

            if (Owner == null)
                return false;

            if (Pile?.Type == PileType.Hand && !YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(Owner, this))
                return false;

            return DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Count > 0;
        }
    }

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
        _fizzleToGraveyard = false;
        _pendingEquipLinkTarget = null;

        if (Owner == null)
        {
            _fizzleToGraveyard = true;
            return Task.CompletedTask;
        }

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseMonsterCard chosen)
        {
            _fizzleToGraveyard = true;
            return Task.CompletedTask;
        }

        if (!DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Contains(chosen))
        {
            _fizzleToGraveyard = true;
            return Task.CompletedTask;
        }

        _pendingEquipLinkTarget = chosen;
        return Task.CompletedTask;
    }

    protected override async Task OnTrapRemainFaceUpInSpellTrapZoneAfterPlayAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        if (_fizzleToGraveyard || _pendingEquipLinkTarget == null)
        {
            await SendThisTrapToGraveyard(choiceContext);
            return;
        }

        await YgoSpellTrapZoneBridge.ActivateEquipLinkTrapAsync(this, _pendingEquipLinkTarget);
        _pendingEquipLinkTarget = null;

        YgoFieldSpellStatAggregator.RefreshMonsterSummonKeywords(Owner);
        DuelMonsterPortraitDecorations.RefreshAllEquipLinkLayers();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Mgc"].UpgradeValueBy(15m);
    }
}
