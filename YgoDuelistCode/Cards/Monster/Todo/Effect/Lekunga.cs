using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Token;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Lekunga : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString BanishSelectionPrompt =
        new("cards", "YGODUELIST-LEKUNGA.banish_selection");

    public Lekunga()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 17,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Plant)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Water | YgoCardPackTags.Ocean | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Lekunga), typeof(Lekunga_Token) };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-LEKUNGA.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && CountWaterMonstersInGraveyard(Owner) >= 2
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0);

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Lekunga || Owner?.Creature?.CombatState == null)
            return;

        Player player = Owner;
        List<BaseMonsterCard> candidates = GraveyardRelic
            .GetGraveyardCards(player)
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Water)
            .ToList();

        if (candidates.Count < 2)
            return;

        var prefs = new CardSelectorPrefs(BanishSelectionPrompt, 2, 2) { Cancelable = true };
        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, player, prefs);
        }
        catch (System.OperationCanceledException)
        {
            return;
        }

        List<BaseMonsterCard> toBanish = selected.OfType<BaseMonsterCard>().Take(2).ToList();
        if (toBanish.Count < 2)
            return;

        foreach (BaseMonsterCard m in toBanish)
            await YgoBanishedService.BanishCard(player, m);

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        if (!await YgoTokenSummon.TrySpecialSummonTokenAsync<Lekunga_Token>(player, choiceContext, defensePosition: false))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    private static int CountWaterMonstersInGraveyard(Player player) =>
        GraveyardRelic.GetGraveyardCards(player).OfType<BaseMonsterCard>().Count(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Water);
}
