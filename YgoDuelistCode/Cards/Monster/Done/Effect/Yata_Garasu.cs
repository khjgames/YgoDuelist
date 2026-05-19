using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Cannot be Special Summoned. When this card attacks, apply Yata Garasu on your leader; gain rewards at the start of your next turn.
/// </summary>
public sealed class Yata_Garasu : SpiritEffectMonsterCard
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";

    private bool ShowYataGarasuPlus => IsUpgradedOrPreviewActive;

    public Yata_Garasu()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Rare,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 2,
            baseDef: 1,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Wind | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Yata_Garasu) };

    public override bool AllowSpecialSummonIgnoringCanSummonDuelMonsterGate => false;

    public override bool UseAlternateUpgradedDescription => true;

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip tip in base.ExtraHoverTips)
                yield return tip;
            if (ShowYataGarasuPlus)
                yield return HoverTipFactory.FromPower<YataGarasuPlusPower>();
            else
                yield return HoverTipFactory.FromPower<YataGarasuPower>();
        }
    }

    public override async Task OnGraveyardRelicAfterAttackOpeningAsync(
        AttackCommand command,
        Player? attackingPlayer,
        BlockingPlayerChoiceContext ctx)
    {
        _ = command;
        _ = ctx;
        if (attackingPlayer?.Creature == null)
            return;
        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(attackingPlayer, this))
            return;

        Creature hero = attackingPlayer.Creature;
        if (ShowYataGarasuPlus)
        {
            if (hero.GetPower<YataGarasuPlusPower>() is { } plus)
                await PowerCmd.ModifyAmount(plus, 1m, hero, this);
            else
                await PowerCmd.Apply<YataGarasuPlusPower>(hero, 1m, hero, this);
            return;
        }

        if (hero.GetPower<YataGarasuPower>() is { } existing)
            await PowerCmd.ModifyAmount(existing, 1m, hero, this);
        else
            await PowerCmd.Apply<YataGarasuPower>(hero, 1m, hero, this);
    }
}
