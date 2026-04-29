using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Token;
using YgoDuelist.YgoDuelistCode.Cards.Trap.Done.Linked;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Reactor_Slime : EffectMonsterCard, IMonsterActivatedEffect, IMonsterSecondActivatedEffect
{
    public Reactor_Slime()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 5,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.God;

    public override Type[] RelatedCards => new[] { typeof(Reactor_Slime), typeof(Slime_Token) };

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-REACTOR_SLIME.activated_effect.description";

    public int SecondActivatedEffectEnergyCost => 0;
    public CardType SecondActivatedEffectCardType => CardType.Skill;
    public TargetType SecondActivatedEffectTarget => TargetType.Self;
    public string SecondActivatedEffectDescriptionLocKey => "YGODUELIST-REACTOR_SLIME.activated_effect_2.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && YgoTokenSummon.MaxTokensThatFit(Owner) >= 2
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0);

    public bool IsSecondActivatedEffectAvailable =>
        Owner?.Creature?.CombatState != null
        && HasMetalReflectInHandDeckOrGraveyard(Owner)
        && YgoPlayerPiles.SpellTrapZone(Owner) != null
        && YgoSpellTrapZoneBridge.HasSpaceForSetOrPlay(
            Owner,
            Owner.Creature.CombatState.CreateCard<Metal_Reflect_Slime>(Owner));

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Reactor_Slime || Owner == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, Owner);
        if (pet == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            if (!DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0))
                break;
            await YgoTokenSummon.TrySpecialSummonTokenAsync<Slime_Token>(Owner, choiceContext, defensePosition: false);
        }

        ReactorSlimeSummonGate.MarkRestricted(Owner);
        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    public async Task OnSecondActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        MonsterCommandRegistry.SetHasUsedSecondActivatedEffectThisTurn(pet, true);

        await CreatureCmd.Kill(pet, force: true);
        CardPile? grave = YgoPlayerPiles.Graveyard(player);
        if (grave != null)
            await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        await Metal_Reflect_Slime.TrySetFromHandDeckOrGraveyardActivatableThisTurnAsync(player, choiceContext);
    }

    private static bool HasMetalReflectInHandDeckOrGraveyard(Player player)
    {
        return EnumerateMetalReflect(player).Any();
    }

    private static IEnumerable<Metal_Reflect_Slime> EnumerateMetalReflect(Player player)
    {
        foreach (var pile in new[] { YgoPlayerPiles.Hand(player), YgoPlayerPiles.Draw(player), YgoPlayerPiles.Discard(player), YgoPlayerPiles.Graveyard(player) })
        {
            if (pile == null)
                continue;
            foreach (CardModel c in pile.Cards)
            {
                if (c is Metal_Reflect_Slime m)
                    yield return m;
            }
        }
    }
}
