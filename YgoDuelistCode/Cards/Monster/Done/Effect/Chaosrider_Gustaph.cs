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

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

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
        && BuildSpellBanishCandidates(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Chaosrider_Gustaph || Owner == null)
            return;

        Player player = Owner;
        if (BuildSpellBanishCandidates(player).Count == 0)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        var prefs = new CardSelectorPrefs(BanishPrompt, 1, 2) { Cancelable = true };
        List<BaseSpellCard> banished = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            prefs,
            () => BuildSpellBanishCandidates(player),
            maxResults: 2);
        if (banished.Count == 0)
            return;

        foreach (BaseSpellCard spell in banished)
            await YgoBanishedService.BanishCard(player, spell);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }
    private static List<BaseSpellCard> BuildSpellBanishCandidates(Player player) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
        .OfType<BaseSpellCard>()
        .ToList();
}
