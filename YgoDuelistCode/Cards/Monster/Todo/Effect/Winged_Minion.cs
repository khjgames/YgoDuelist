using System;
using System.Collections.Generic;
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

public sealed class Winged_Minion : EffectMonsterCard, IMonsterActivatedEffect, IMonsterActivatedEffectPrePlaySelection
{
    private static readonly LocString SilentFiendPickPrompt =
        new LocString("cards", "YGODUELIST-WINGED_MINION.activated_effect.selection");

    public Winged_Minion()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 7,
            baseDef: 7,
            baseMgc: 7,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Winged_Minion),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<WingedMinionTributeAtkPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-WINGED_MINION.activated_effect.description";

    public bool IsActivatedEffectAvailable => IsAnotherFiendControlled(Owner);

    public override bool IsActivatedEffectAvailableInCommandContext(Player? commandOwner) => IsAnotherFiendControlled(commandOwner);

    /// <summary>Used when the source card has not yet set <see cref="CardModel.Owner"/>.</summary>
    public bool IsAnotherFiendControlled(Player? playerContext)
    {
        Player? player = playerContext ?? Owner;
        if (player?.PlayerCombatState == null)
            return false;

        return BuildFiendCandidates(player, this).Count > 0;
    }

    public async Task<bool> TryPrepareActivatedEffectPlayAsync(Player player, NormalMonsterCard source)
    {
        List<BaseMonsterCard> candidates = BuildFiendCandidates(player, source);
        if (candidates.Count == 0)
            return false;

        return await YgoActivatedEffectTributeSelection.TryPrepareSingleTributeAsync(
            player,
            source,
            candidates.Cast<CardModel>().ToList(),
            SilentFiendPickPrompt);
    }

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.PlayerCombatState == null)
            return;

        if (!ActivatedEffectTributeSelectionPayload.TryTakePending(source, out var chosenFiend) || chosenFiend == null)
            return;

        Creature? sourcePet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (sourcePet == null)
            return;

        Creature? targetPet = TributeSummonSelection.ResolvePetForFieldCard(player, chosenFiend);
        if (targetPet == null || !targetPet.IsAlive || ReferenceEquals(chosenFiend, source))
            return;

        if (!DuelMonsterFieldRegistry.ContainsFieldMonster(player, chosenFiend))
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(sourcePet, true);

        await CreatureCmd.Kill(sourcePet, force: true);
        var grave = YgoPlayerPiles.Graveyard(player);
        if (grave != null)
            await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        targetPet = TributeSummonSelection.ResolvePetForFieldCard(player, chosenFiend);
        if (targetPet == null || !targetPet.IsAlive || player.Creature == null)
            return;

        decimal atkBonus = source.DynamicVars["Mgc"].BaseValue;
        await PowerCmd.Apply<WingedMinionTributeAtkPower>(targetPet, atkBonus, player.Creature, source);
    }

    private static List<BaseMonsterCard> BuildFiendCandidates(Player player, NormalMonsterCard source)
    {
        return DuelMonsterFieldRegistry.OrderedFieldMonsters(player)
            .Where(c => !ReferenceEquals(c, source) && c.DuelMonsterRace == DuelMonsterRace.Fiend)
            .ToList();
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 11m;
    }
}
