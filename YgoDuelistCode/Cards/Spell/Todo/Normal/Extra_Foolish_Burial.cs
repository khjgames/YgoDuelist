using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>
/// Send 1 Fusion monster from your Extra Deck to the Graveyard. Pay life first using the same formula as Jirai Gumo wrong-call damage:
/// blockable damage equal to <c>floor((current HP − 1) / 2)</c> (0 when current HP is 2 or less).
/// </summary>
public sealed class Extra_Foolish_Burial : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public Extra_Foolish_Burial()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Extra_Foolish_Burial) };

    protected override bool IsPlayable =>
        base.IsPlayable &&
        Owner != null &&
        PlayerRunExtraDeck.IsYgoDuelistPlayer(Owner) &&
        GetFusionMonstersInExtraDeck(Owner).Any();

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        var candidates = GetFusionMonstersInExtraDeck(player).ToList();
        if (candidates.Count == 0)
            return false;

        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates,
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        var chosen = selected.FirstOrDefault() as FusionMonsterCard;
        if (chosen == null)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player?.Creature == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not FusionMonsterCard chosen)
            return;

        CardPile? extra = PlayerRunExtraDeck.GetPileIfExists(player);
        if (extra == null || !extra.Cards.Contains(chosen))
            return;

        decimal hp = player.Creature.CurrentHp;
        decimal lifeCostDamage = Math.Max(0m, Math.Floor((hp - 1m) / 2m));
        if (lifeCostDamage > 0m)
            await CreatureCmd.Damage(choiceContext, player.Creature, lifeCostDamage, ValueProp.Unpowered, player.Creature, this);

        var graveyardPile = GraveyardPile.CustomType.GetPile(player);
        if (graveyardPile == null)
            return;

        await CardPileCmd.Add(
            new[] { chosen },
            graveyardPile,
            CardPilePosition.Top,
            chosen,
            false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static IEnumerable<FusionMonsterCard> GetFusionMonstersInExtraDeck(Player player)
    {
        CardPile? extra = PlayerRunExtraDeck.GetPileIfExists(player);
        if (extra == null)
            return Enumerable.Empty<FusionMonsterCard>();

        return extra.Cards.OfType<FusionMonsterCard>();
    }
}
