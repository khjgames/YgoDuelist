using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>Activate Effect: Tribute this monster; gain <c>Mgc</c> Block (6 base, 10 when upgraded).</summary>
public sealed class Maryokutai : EffectMonsterCard, IMonsterActivatedEffect
{
    public Maryokutai()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 3,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 9,
            baseDef: 9,
            baseMgc: 6,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Water | YgoCardPackTags.Starter;

    public override Type[] RelatedCards => new[] { typeof(Maryokutai) };

    public int ActivatedEffectEnergyCost => 0;

    public CardType ActivatedEffectCardType => CardType.Skill;

    public TargetType ActivatedEffectTarget => TargetType.Self;

    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-MARYOKUTAI.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && !FaceDown
        && MonsterActivatedEffectRuntime.FindPetForSourceMonster(this, Owner) != null;

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        if (source is not Maryokutai)
            return;

        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        if (player?.Creature == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        decimal block = source.DynamicVars["Mgc"].BaseValue;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
        await CreatureCmd.Kill(pet, force: true);

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        if (gy != null)
            await CardPileCmd.Add(new[] { source }, gy, CardPilePosition.Top, source, false);

        if (block > 0m)
            await CreatureCmd.GainBlock(player.Creature, block, default, cardPlay);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 10m;
    }
}
