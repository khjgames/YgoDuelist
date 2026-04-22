using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Trap_Master : EffectMonsterCard, IMonsterFlipEffect
{
    private static readonly LocString FlipTargetPrompt =
        new("cards", "YGODUELIST-TRAP_MASTER.flip_destroy_trap_select");

    public Trap_Master()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 5,
            baseDef: 11,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Warrior | YgoCardPackTags.Trap;

    public override Type[] RelatedCards => new[] { typeof(Trap_Master) };

    private bool ShowNextTrapDiscountPlus =>
        IsUpgraded || UpgradePreviewType != CardUpgradePreviewType.None;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            if (ShowNextTrapDiscountPlus)
                yield return HoverTipFactory.FromPower<NextTrapDiscountPowerPlus>();
            else
                yield return HoverTipFactory.FromPower<NextTrapDiscountPower>();
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Trap_Master || Owner?.Creature?.CombatState == null)
            return;

        CombatState cs = Owner.Creature.CombatState;
        var candidates = new List<BaseTrapCard>();
        YgoFlipSpellTrapFieldEffects.CollectTrapsInAllSpellTrapZones(cs, candidates);
        if (candidates.Count == 0)
            return;

        IEnumerable<CardModel> pick;
        try
        {
            pick = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates.Cast<CardModel>().ToList(),
                Owner,
                new CardSelectorPrefs(FlipTargetPrompt, 1, 1)
                {
                    RequireManualConfirmation = true,
                    Cancelable = true
                });
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (pick.FirstOrDefault() is not BaseTrapCard victim)
            return;

        bool destroyed = await YgoFlipSpellTrapFieldEffects.TrySendSpellTrapOnFieldToGraveyardAsync(victim, this);
        if (!destroyed || Owner.Creature == null)
            return;

        if (IsUpgraded)
            await PowerCmd.Apply<NextTrapDiscountPowerPlus>(Owner.Creature, 1m, Owner.Creature, this);
        else
            await PowerCmd.Apply<NextTrapDiscountPower>(Owner.Creature, 1m, Owner.Creature, this);
        YgoSpellTrapDiscountEnergyRefresh.ForPlayer(Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 2m;
    }
}
