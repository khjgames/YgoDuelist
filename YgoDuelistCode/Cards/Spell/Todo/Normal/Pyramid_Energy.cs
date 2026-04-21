using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Command;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Pyramid_Energy : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    private const int OptionAtk = 0;
    private const int OptionDef = 1;

    private const decimal AtkBonus = 2m;
    private const decimal DefBonus = 5m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Mgc", AtkBonus), new DynamicVar("Mgc2", DefBonus) };

    public Pyramid_Energy()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Spell;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            yield return HoverTipFactory.FromPower<PyramidEnergyAtkBonusPower>();
            yield return HoverTipFactory.FromPower<PyramidEnergyDefBonusPower>();
        }
    }

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        if (player.Creature?.CombatState is not CombatState cs)
            return false;

        YgoTransientSpellOptionCommandCard atkOpt = YgoTransientSpellOptionCommandCard.Create(
            cs,
            player,
            OptionAtk,
            "YGODUELIST-PYRAMID_ENERGY_OPT_ATK.title",
            "YGODUELIST-PYRAMID_ENERGY_OPT_ATK.description",
            this,
            "pyramid_energy.png");

        YgoTransientSpellOptionCommandCard defOpt = YgoTransientSpellOptionCommandCard.Create(
            cs,
            player,
            OptionDef,
            "YGODUELIST-PYRAMID_ENERGY_OPT_DEF.title",
            "YGODUELIST-PYRAMID_ENERGY_OPT_DEF.description",
            this,
            "pyramid_energy.png");

        var options = new List<CardModel> { atkOpt, defOpt };
        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                options,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        CardModel? pick = selected.FirstOrDefault();
        if (pick is not YgoTransientSpellOptionCommandCard chosen)
            return false;

        YgoPrePlayOptionIdPayload.SetPending(sourceCard, chosen.OptionId);
        return true;
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        if (!YgoPrePlayOptionIdPayload.TryTakePending(this, out int optionId))
            return;

        // Buff lives on the player so duel monsters still in hand (summoned later this turn) get the bonus.
        if (optionId == OptionAtk)
            await PowerCmd.Apply<PyramidEnergyAtkBonusPower>(Owner.Creature, AtkBonus, Owner.Creature, this);
        else
            await PowerCmd.Apply<PyramidEnergyDefBonusPower>(Owner.Creature, DefBonus, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
