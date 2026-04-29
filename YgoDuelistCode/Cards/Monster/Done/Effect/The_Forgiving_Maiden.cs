using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Fusion;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>
/// Activate Effect: Tribute this monster. Your duel monsters gain Unyielding {Mgc} until end of your turn (Die For You spill floor).
/// </summary>
public sealed class The_Forgiving_Maiden : EffectMonsterCard, IMonsterActivatedEffect
{
    private static readonly CardKeyword UnyieldingKeyword = (CardKeyword)20062;

    public The_Forgiving_Maiden()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Light,
            baseAtk: 8,
            baseDef: 20,
            baseMgc: 1,
            duelMonsterRace: DuelMonsterRace.Fairy)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Light | YgoCardPackTags.Starter | YgoCardPackTags.Heal;

    public override Type[] RelatedCards =>
        new[]
        {
            typeof(The_Forgiving_Maiden),
            typeof(St_Joan),
        };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<UnyieldingPower>();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            foreach (CardKeyword kw in base.CanonicalKeywords)
                yield return kw;
            yield return UnyieldingKeyword;
        }
    }

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey =>
        "YGODUELIST-THE_FORGIVING_MAIDEN.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && !FaceDown
        && MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner) != null;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not The_Forgiving_Maiden)
            return;

        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.Creature == null)
            return;

        Creature? selfPet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (selfPet == null)
            return;

        decimal stacks = source.DynamicVars["Mgc"].BaseValue;
        if (stacks <= 0m)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(selfPet, true);
        await CreatureCmd.Kill(selfPet, force: true);

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy != null)
            await CardPileCmd.Add(new[] { source }, gy, CardPilePosition.Top, source, false);

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive || pet.Monster is not DuelMonsterModel)
                continue;
            await PowerCmd.Apply<ForgivingMaidenEndTurnUnyieldingPower>(
                pet,
                stacks,
                player.Creature,
                source);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 3m;
    }
}
