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
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;

public sealed class Multiply : BaseSpellCard, IYgoPrePlayCancelableGridSelection
{
    public Multiply()
        : base(cost: 1, rarity: CardRarity.Common, target: TargetType.Self, duelMonsterRace: DuelMonsterRace.SpellQuickPlay)
    {
    }
    public override Type[] BundledCards => new[] { typeof(Kuriboh) };

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Dark | YgoCardPackTags.Fiend | YgoCardPackTags.Spell | YgoCardPackTags.Bundled;

    public override Type[] RelatedCards => new[] { typeof(Multiply), typeof(Kuriboh_Token) };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Kuriboh), typeof(Kuriboh_Token));

    protected override bool IsPlayable =>
        base.IsPlayable
        && Owner != null
        && BuildKuribohCandidates(Owner).Count > 0;

    public async Task<bool> TryPreparePrePlayCancelableGridAsync(Player player, CardModel sourceCard)
    {
        List<Kuriboh> field = BuildKuribohCandidates(player);
        if (field.Count == 0)
            return false;

        return await YgoPrePlayGridSelection.TryPrepareSingleCardPayloadAsync<Kuriboh>(
            player,
            sourceCard,
            field.Cast<CardModel>().ToList(),
            YgoCancelableConfirmGridPrefs.ForSinglePick(SelectionScreenPrompt),
            rebuildCanonicalForRemoteApply: () => BuildKuribohCandidates(player).Cast<CardModel>().ToList());
    }

    protected override async Task OnSpellPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        if (!YgoPrePlaySelectedCardPayload.TryTakePending(this, out CardModel? picked) || picked is not Kuriboh kuriboh)
            return;

        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(Owner, kuriboh))
            return;

        Creature? tributePet = TributeSummonSelection.ResolvePetForFieldCard(Owner, kuriboh);
        if (tributePet == null || !tributePet.IsAlive)
            return;

        await CreatureCmd.Kill(tributePet, force: true);
        CardPile? grave = YgoPlayerPiles.Graveyard(Owner);
        if (grave != null)
            await CardPileCmd.Add(new[] { kuriboh }, grave, CardPilePosition.Top, kuriboh, false);

        int n = YgoTokenSummon.MaxTokensThatFit(Owner);
        for (int i = 0; i < n; i++)
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Kuriboh_Token>(Owner, choiceContext, defensePosition: true);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private static List<Kuriboh> BuildKuribohCandidates(Player player) =>
        DuelMonsterFieldRegistry.OrderedFieldMonstersOfType<Kuriboh>(player).ToList();
}
