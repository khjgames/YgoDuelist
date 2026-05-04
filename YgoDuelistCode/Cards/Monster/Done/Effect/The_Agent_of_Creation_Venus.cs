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
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using MonsterActivatedEffectRuntime = YgoDuelist.YgoDuelistCode.Cards.Core.MonsterActivatedEffectRuntime;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Activate Effect: pay {Mgc} HP; Special Summon 1 Mystical Shine Ball from hand or deck.</summary>
public sealed class The_Agent_of_Creation_Venus : EffectMonsterCard, IMonsterActivatedEffect
{
    public The_Agent_of_Creation_Venus()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 16,
            baseDef: 0,
            baseMgc: 5,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Draw;
    public override Type[] RelatedCards => new[] { typeof(The_Agent_of_Creation_Venus), typeof(Mystical_Shine_Ball) };

    protected override Type[] PreviewReferencedCardTypes =>
        YgoPreviewReferencedCardTypes.Merged(GetType(), typeof(Mystical_Shine_Ball));

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-THE_AGENT_OF_CREATION_VENUS.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(Owner, 0)
        && BuildShineBallCandidates(Owner).Count > 0;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.Creature == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        List<Mystical_Shine_Ball> candidates = BuildShineBallCandidates(player);
        if (candidates.Count == 0 || !DuelMonsterSummon.HasRoomForDuelSummonAfterReleasing(player, 0))
            return;

        int hpCost = (int)source.DynamicVars["Mgc"].BaseValue;
        if (hpCost > 0)
        {
            if (player.Creature.CurrentHp <= hpCost)
                return;
            await CreatureCmd.SetCurrentHp(player.Creature, player.Creature.CurrentHp - hpCost);
        }

        Mystical_Shine_Ball? chosen = await YgoOrderedCardSelection.TryChooseSingleAsync(
            choiceContext,
            player,
            new CardSelectorPrefs(new("cards", "YGODUELIST-THE_AGENT_OF_CREATION_VENUS.select_shine_ball"), 1, 1)
            {
                RequireManualConfirmation = true,
                Cancelable = true
            },
            () => BuildShineBallCandidates(player));
        if (chosen == null)
            return;

        chosen.FaceDown = false;
        if (!await DuelMonsterSummon.TrySummonDuelMonsterSpecial(player, chosen, choiceContext))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    private static List<Mystical_Shine_Ball> BuildShineBallCandidates(Player player) => YgoPlayerPiles
        .OrderedCardsOfTypeFromHandDrawDiscard<Mystical_Shine_Ball>(player)
        .Where(m => m.CanSummonDuelMonster)
        .ToList();

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
