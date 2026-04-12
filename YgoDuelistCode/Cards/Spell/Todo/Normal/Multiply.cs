using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Multiply : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public Multiply()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }
    public override Type[] BundledCards => new[] { typeof(Kuriboh) };

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Spell;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner).OfType<Kuriboh>().Any();

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<Kuriboh> field = DuelMonsterFieldRegistry.GetFieldMonsters(player).OfType<Kuriboh>().ToList();
        if (field.Count == 0)
            return false;

        var prefs = YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt);
        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), field, player, prefs);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        var chosen = selected.FirstOrDefault() as Kuriboh;
        if (chosen == null)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not Kuriboh kuriboh)
            return;

        if (!DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Contains(kuriboh))
            return;

        Creature? tributePet = TributeSummonSelection.ResolvePetForFieldCard(Owner, kuriboh);
        if (tributePet == null || !tributePet.IsAlive)
            return;

        await CreatureCmd.Kill(tributePet, force: true);
        CardPile? grave = GraveyardPile.CustomType.GetPile(Owner);
        if (grave != null)
            await CardPileCmd.Add(new[] { kuriboh }, grave, CardPilePosition.Top, kuriboh, false);

        int n = YgoTokenSummon.MaxTokensThatFit(Owner);
        for (int i = 0; i < n; i++)
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Kuriboh_Token>(Owner, choiceContext, defensePosition: true);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
