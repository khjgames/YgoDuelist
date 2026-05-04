using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Night_Assailant : EffectMonsterCard, IMonsterFlipEffect, IYgoAfterMonsterMovedToGraveyardFromHandOrDraw
{
    private const string ConduitImgBbcode = "[img]res://YgoDuelist/images/card_frames/conduit_icon.png[/img]";
    private static readonly LocString FlipPrompt = new("cards", "YGODUELIST-NIGHT_ASSAILANT.flip_destroy_select");
    private static readonly LocString GyFlipReturnPrompt = new("cards", "YGODUELIST-NIGHT_ASSAILANT.gy_flip_return_select");

    public Night_Assailant()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 2,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override bool UseAlternateUpgradedDescription => true;

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Night_Assailant) };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new EnergyVar(0) }.Concat(base.CanonicalVars);

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not Night_Assailant || Owner == null)
            return;

        await YgoFlipReturnOwnFieldMonsterToHand.RunFlipDestroyOneOtherControlledWithConduitAsync(
            choiceContext,
            Owner,
            self,
            FlipPrompt,
            IsUpgraded);
    }

    public async Task OnAfterMovedToGraveyardFromHandOrDrawAsync(Player player, PileType fromPile)
    {
        _ = fromPile;
        if (player.Creature?.CombatState == null || player.Creature.Side != CombatSide.Player)
            return;
        if (!YgoPlayerPiles.GraveyardContains(player, this))
            return;

        List<BaseMonsterCard> candidates = BuildFlipTargets(player, this);

        if (candidates.Count == 0)
            return;

        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (hand == null)
            return;

        BaseMonsterCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking(),
            player,
            new CardSelectorPrefs(GyFlipReturnPrompt, 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildFlipTargets(player, this));
        if (chosen == null)
            return;
        if (!YgoPlayerPiles.GraveyardContains(player, chosen))
            return;

        await CardPileCmd.Add(new[] { chosen }, hand, CardPilePosition.Top, chosen, false);
    }

    private static List<BaseMonsterCard> BuildFlipTargets(Player player, CardModel sourceCard) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
        .Where(c => c is BaseMonsterCard && c is IMonsterFlipEffect && !ReferenceEquals(c, sourceCard))
        .OfType<BaseMonsterCard>()
        .ToList();

    protected override void AddExtraArgsToDescription(LocString description) =>
        description.Add("conduitIcon", ConduitImgBbcode);

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
