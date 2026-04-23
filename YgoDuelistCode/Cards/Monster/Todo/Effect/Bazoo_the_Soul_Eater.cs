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
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Bazoo_the_Soul_Eater : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString BanishPrompt = new("cards", "YGODUELIST-BAZOO_THE_SOUL_EATER.banish_selection");

    public Bazoo_the_Soul_Eater()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 16,
            baseDef: 9,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Beast)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Banish;

    public override YgoCardArchetype CardArchetypes => YgoCardArchetype.GrowthType;

    public override Type[] RelatedCards => new[] { typeof(Bazoo_the_Soul_Eater) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BazooSoulEaterTempAtkPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-BAZOO_THE_SOUL_EATER.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null && BuildBanishCandidates(Owner).Count >= 1;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Bazoo_the_Soul_Eater bazoo || Owner?.Creature == null)
            return;

        Player player = Owner;
        if (BuildBanishCandidates(player).Count == 0)
            return;

        var prefs = new CardSelectorPrefs(BanishPrompt, 1, 3) { Cancelable = true };
        List<BaseMonsterCard> banished = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            prefs,
            () => BuildBanishCandidates(player),
            maxResults: 3);
        if (banished.Count == 0)
            return;

        foreach (BaseMonsterCard m in banished)
            await YgoBanishedService.BanishCard(player, m);

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(bazoo, player);
        if (pet == null)
            return;

        decimal per = bazoo.DynamicVars["Mgc"].BaseValue;
        decimal add = per * banished.Count;
        if (pet.GetPower<BazooSoulEaterTempAtkPower>() is { } existing)
            await PowerCmd.ModifyAmount(existing, add, player.Creature, bazoo);
        else
            await PowerCmd.Apply<BazooSoulEaterTempAtkPower>(pet, add, player.Creature, bazoo);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 2m;
    }

    private static List<BaseMonsterCard> BuildBanishCandidates(Player player) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
        .OfType<BaseMonsterCard>()
        .ToList();
}
