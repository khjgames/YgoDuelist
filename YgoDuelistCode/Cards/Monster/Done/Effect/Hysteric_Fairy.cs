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
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Hysteric_Fairy : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString TributePrompt = new("combat_messages", "TRIBUTE_SUMMON_SELECT");

    public Hysteric_Fairy()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 18,
            baseDef: 5,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Heal | YgoCardPackTags.Spell;

    public override Type[] RelatedCards => new[] { typeof(Hysteric_Fairy) };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-HYSTERIC_FAIRY.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && TributeSummonSelection.BuildTributeCandidateCards(Owner).Count(c => !ReferenceEquals(c, this)) >= 2;

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        if (player.PlayerCombatState?.Pets == null)
            return false;

        var candidates = TributeSummonSelection
            .BuildTributeCandidateCards(player)
            .Where(c => !ReferenceEquals(c, source))
            .ToList();

        if (candidates.Count < 2)
            return false;

        return await YgoActivatedEffectTributeSelection.TryPrepareExactTributesAsync(
            player,
            source,
            candidates,
            tributeCount: 2,
            TributePrompt);
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        if (!ObeliskActivatedTributePayload.TryTakePending(source, out var tributes) || tributes == null)
            return;

        foreach (BaseMonsterCard tributeCard in tributes)
        {
            Creature? tributePet = TributeSummonSelection.ResolvePetForFieldCard(player, tributeCard);
            if (tributePet == null || !tributePet.IsAlive)
                return;

            await CreatureCmd.Kill(tributePet, force: true);

            CardPile? graveyard = YgoPlayerPiles.Graveyard(player);
            if (graveyard != null)
                await CardPileCmd.Add(new[] { tributeCard }, graveyard, CardPilePosition.Top, tributeCard, false);
        }

        decimal heal = source.DynamicVars["Mgc"].BaseValue;
        if (heal > 0m)
            await CreatureCmd.Heal(player.Creature, heal);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
