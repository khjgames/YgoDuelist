using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Fairy_Guardian : EffectMonsterCard, IMonsterActivatedEffect
{
    public override int AttackPortionCount => 2;
    private static readonly LocString SpellPickPrompt =
        new LocString("cards", "YGODUELIST-FAIRY_GUARDIAN.activated_effect.selection");

    public Fairy_Guardian()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 10,
            baseDef: 10,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Draw | YgoCardPackTags.Spell;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Fairy_Guardian),
    };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-FAIRY_GUARDIAN.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null &&
        BuildSpellTargets(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        var player = source.Owner;
        if (player == null)
            return;

        var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source);
        if (pet == null)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);

        await CreatureCmd.Kill(pet, force: true);
        var grave = YgoPlayerPiles.Graveyard(player);
        if (grave != null)
            await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        if (BuildSpellTargets(player).Count == 0)
            return;

        BaseSpellCard? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(SpellPickPrompt, 1, 1),
            () => BuildSpellTargets(player));
        if (chosen == null)
            return;

        var drawPile = YgoPlayerPiles.Draw(player);
        if (drawPile == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, drawPile, CardPilePosition.Bottom, source, false);
    }

    protected override void OnUpgrade() => base.OnUpgrade();

    private static List<BaseSpellCard> BuildSpellTargets(Player player) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
        .OfType<BaseSpellCard>()
        .ToList();
}
