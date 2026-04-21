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
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Spirit_Ryu : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString HandPrompt = new("cards", "YGODUELIST-SPIRIT_RYU.hand_select");

    public Spirit_Ryu()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Wind,
            baseAtk: 10,
            baseDef: 10,
            baseMgc: 10,
            duelMonsterRace: DuelMonsterRace.Dragon)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Wind | YgoCardPackTags.Dragon | YgoCardPackTags.Burn;

    public override Type[] RelatedCards => new[] { typeof(Spirit_Ryu) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<SpiritRyuTempAtkDefPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-SPIRIT_RYU.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && GetDragonMonstersInHand(Owner).Count > 0;

    private static List<BaseMonsterCard> GetDragonMonstersInHand(Player player)
    {
        CardPile? hand = PileType.Hand.GetPile(player);
        if (hand == null)
            return new List<BaseMonsterCard>();

        return hand.Cards
            .OfType<BaseMonsterCard>()
            .Where(m => m.DuelMonsterRace == DuelMonsterRace.Dragon)
            .ToList();
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Spirit_Ryu ryu || Owner?.Creature == null)
            return;

        Player player = Owner;
        List<BaseMonsterCard> pool = GetDragonMonstersInHand(player);
        if (pool.Count == 0)
            return;

        List<CardModel> candidates = TributeSummonGridSelect.StabilizeHandPileCandidates(pool.Cast<CardModel>());
        IEnumerable<CardModel> pick;
        try
        {
            pick = await TributeSummonGridSelect.FromSimpleGridCombat(
                choiceContext,
                candidates,
                player,
                new CardSelectorPrefs(HandPrompt, 1, 1) { Cancelable = true },
                rebuildCanonicalForRemoteApply: () =>
                    TributeSummonGridSelect.StabilizeHandPileCandidates(GetDragonMonstersInHand(player).Cast<CardModel>()));
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (pick.FirstOrDefault() is not BaseMonsterCard chosen)
            return;

        CardPile? discard = PileType.Discard.GetPile(player);
        if (discard == null)
            return;

        await CardPileCmd.Add(new[] { chosen }, discard, CardPilePosition.Top, chosen, false);

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(ryu, player);
        if (pet == null)
            return;

        decimal bonus = ryu.DynamicVars["Mgc"].BaseValue;
        if (bonus <= 0m)
            return;

        if (pet.GetPower<SpiritRyuTempAtkDefPower>() is { } existing)
            await PowerCmd.ModifyAmount(existing, bonus, player.Creature, ryu);
        else
            await PowerCmd.Apply<SpiritRyuTempAtkDefPower>(pet, bonus, player.Creature, ryu);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 13m;
    }
}
