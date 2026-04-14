using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Chaosrider_Gustaph : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString BanishPrompt = new("cards", "YGODUELIST-CHAOSRIDER_GUSTAPH.banish_spells");

    public Chaosrider_Gustaph()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 14,
            baseDef: 15,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Warrior | YgoCardPackTags.Banish;

    public override Type[] RelatedCards => new[] { typeof(Chaosrider_Gustaph) };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-CHAOSRIDER_GUSTAPH.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && GraveyardRelic.GetGraveyardCards(Owner).OfType<BaseSpellCard>().Any();

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Chaosrider_Gustaph || Owner == null)
            return;

        Player player = Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        List<BaseSpellCard> pool = GraveyardRelic
            .GetGraveyardCards(player)
            .OfType<BaseSpellCard>()
            .ToList();
        if (pool.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(BanishPrompt, 1, 2) { Cancelable = true };
        IEnumerable<CardModel> pick;
        try
        {
            pick = await CardSelectCmd.FromSimpleGrid(choiceContext, pool, player, prefs);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        List<BaseSpellCard> banished = pick.OfType<BaseSpellCard>().Distinct().Take(2).ToList();
        if (banished.Count == 0)
            return;

        foreach (BaseSpellCard spell in banished)
            await YgoBanishedService.BanishCard(player, spell);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }
}
