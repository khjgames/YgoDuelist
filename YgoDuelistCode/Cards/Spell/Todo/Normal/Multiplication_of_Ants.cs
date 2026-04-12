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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Todo.Normal;

public sealed class Multiplication_of_Ants : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public Multiplication_of_Ants()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellNormal)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.Insect | YgoCardPackTags.Spell;

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Any(m => m.DuelMonsterRace == DuelMonsterRace.Insect);

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<BaseMonsterCard> field = DuelMonsterFieldRegistry
            .GetFieldMonsters(player)
            .Where(m => m.DuelMonsterRace == DuelMonsterRace.Insect)
            .ToList();
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

        var chosen = selected.FirstOrDefault() as BaseMonsterCard;
        if (chosen == null)
            return false;

        YgoPrePlaySelectedCardPayload.SetPending(sourceCard, chosen);
        return true;
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not BaseMonsterCard tribute)
            return;

        if (tribute.DuelMonsterRace != DuelMonsterRace.Insect || !DuelMonsterFieldRegistry.GetFieldMonsters(Owner).Contains(tribute))
            return;

        Creature? tributePet = TributeSummonSelection.ResolvePetForFieldCard(Owner, tribute);
        if (tributePet == null || !tributePet.IsAlive)
            return;

        await CreatureCmd.Kill(tributePet, force: true);
        CardPile? grave = GraveyardPile.CustomType.GetPile(Owner);
        if (grave != null)
            await CardPileCmd.Add(new[] { tribute }, grave, CardPilePosition.Top, tribute, false);

        for (int i = 0; i < 2; i++)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0))
                break;
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Army_Ant_Token>(Owner, choiceContext, defensePosition: false);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
