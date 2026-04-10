using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

/// <summary>
/// Add 1 Field Spell from your draw or discard pile to your hand (cancelable grid before the spell resolves).
/// </summary>
public sealed class Terraforming : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public Terraforming()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Spell | YgoCardPackTags.Draw;

    protected override bool IsPlayable =>
        base.IsPlayable && Owner != null && GetFieldSpellsInDrawOrDiscard(Owner).Count > 0;

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<BaseFieldSpellCard> candidates = GetFieldSpellsInDrawOrDiscard(player);
        if (candidates.Count == 0)
            return false;

        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                candidates.Cast<CardModel>().ToList(),
                player,
                prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        CardModel? first = selected.FirstOrDefault();
        if (first is not BaseFieldSpellCard chosen)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner;
        if (player == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseFieldSpellCard chosen)
            return;

        CardPile? draw = PileType.Draw.GetPile(player);
        CardPile? discard = PileType.Discard.GetPile(player);
        bool inDraw = draw != null && draw.Cards.Contains(chosen);
        bool inDiscard = discard != null && discard.Cards.Contains(chosen);
        if (!inDraw && !inDiscard)
            return;

        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return;

        await CardPileCmd.Add(
            new CardModel[] { chosen },
            hand,
            CardPilePosition.Top,
            chosen,
            false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static List<BaseFieldSpellCard> GetFieldSpellsInDrawOrDiscard(Player player)
    {
        var list = new List<BaseFieldSpellCard>();

        CardPile? draw = PileType.Draw.GetPile(player);
        if (draw != null)
        {
            foreach (CardModel c in draw.Cards)
            {
                if (c is BaseFieldSpellCard fs)
                    list.Add(fs);
            }
        }

        CardPile? discard = PileType.Discard.GetPile(player);
        if (discard != null)
        {
            foreach (CardModel c in discard.Cards)
            {
                if (c is BaseFieldSpellCard fs)
                    list.Add(fs);
            }
        }

        return list;
    }
}
