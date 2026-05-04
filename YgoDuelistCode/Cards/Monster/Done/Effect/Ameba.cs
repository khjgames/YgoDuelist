using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Ameba : EffectMonsterCard, IMonsterActivatedEffect
{
    public Ameba()
        : base(
            cost: 0,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 3,
            baseDef: 3,
            baseMgc: 20,
            duelMonsterRace: DuelMonsterRace.Aqua)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Burn | YgoCardPackTags.Ocean | YgoCardPackTags.Water;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Ameba),
    };

    public int ActivatedEffectEnergyCost => 2;
    public CardType ActivatedEffectCardType => CardType.Attack;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-AMEBA.activated_effect.description";

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        var player = source.Owner;
        if (player == null || cardPlay.Target == null)
            return;

        var pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source);
        if (pet == null)
            return;

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);

        await CreatureCmd.Kill(pet, force: true);
        var grave = YgoPlayerPiles.Graveyard(player);
        if (grave != null)
            await CardPileCmd.Add(new[] { source }, grave, CardPilePosition.Top, source, false);

        await DamageCmd.Attack(source.DynamicVars["Mgc"].BaseValue)
            .FromCard(source)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 25m;
    }
}
