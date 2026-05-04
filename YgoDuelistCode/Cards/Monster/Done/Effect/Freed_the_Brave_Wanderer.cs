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

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Once per turn: pay 1 energy, banish 2 LIGHT monsters from your Graveyard, inflict Blight on target enemy equal to this card's ATK (see Effect_Monsters_TODO).
/// </summary>
public sealed class Freed_the_Brave_Wanderer : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly LocString BanishPrompt = new("cards", "YGODUELIST-FREED_THE_BRAVE_WANDERER.banish_light");

    public Freed_the_Brave_Wanderer()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 17,
            baseDef: 12,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Warrior | YgoCardPackTags.Burn;
    public override Type[] RelatedCards => new[] { typeof(Freed_the_Brave_Wanderer) };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 1;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-FREED_THE_BRAVE_WANDERER.activated_effect.description";

    public bool IsActivatedEffectAvailable
    {
        get
        {
            if (Owner?.Creature?.CombatState == null)
                return false;
            Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner);
            if (pet == null || !MonsterCommandRegistry.TryGet(pet, out var cmd) || cmd.HasUsedActivatedEffectThisTurn)
                return false;
            if (BuildLightGraveyardCandidates(Owner).Count < 2)
                return false;
            return Owner.Creature.CombatState.HittableEnemies.Any(e => e.IsAlive);
        }
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.Creature?.CombatState == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        if (BuildLightGraveyardCandidates(player).Count < 2)
            return;

        List<BaseMonsterCard> banished = await YgoOrderedCardSelection.TryChooseManyAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(BanishPrompt, 2, 2) { Cancelable = true },
            () => BuildLightGraveyardCandidates(player),
            maxResults: 2);
        if (banished.Count < 2)
            return;

        foreach (BaseMonsterCard m in banished)
            await YgoBanishedService.BanishCard(player, m);

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        if (source is not Freed_the_Brave_Wanderer freed)
            return;

        IReadOnlyCollection<BaseMonsterCard> field = DuelMonsterFieldRegistry.OrderedFieldMonsters(player);
        int blight = freed.CalcDuelMonsterStats(field).Atk;
        if (blight > 0)
            await PowerCmd.Apply<BlightPower>(target, blight, player.Creature, freed);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    private static List<BaseMonsterCard> BuildLightGraveyardCandidates(Player player) => YgoMpCombatOrder
        .CardsSnapshotOrderedForMp(YgoPlayerPiles.GraveyardCards(player))
        .OfType<BaseMonsterCard>()
        .Where(m => m.DuelMonsterAttribute == DuelMonsterAttribute.Light)
        .ToList();
}
