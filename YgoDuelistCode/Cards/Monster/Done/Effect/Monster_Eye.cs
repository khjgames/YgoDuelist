using System;
using System.Linq;
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
using YgoDuelist.YgoDuelistCode.Cards.Spell.Done.Normal;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Monster_Eye : EffectMonsterCard, IMonsterActivatedEffect
{
    public Monster_Eye()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Common,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 1,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 2,
            baseDef: 3,
            baseMgc: 9,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Fusion | YgoCardPackTags.Fiend | YgoCardPackTags.Dark;

    public override Type[] RelatedCards => new[]
    {
        typeof(Monster_Eye),
        typeof(Polymerization),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<DoomPower>();
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Skill;
    public TargetType ActivatedEffectTarget => TargetType.Self;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-MONSTER_EYE.activated_effect.description";

    public bool IsActivatedEffectAvailable =>
        Owner != null
        && YgoPlayerPiles.GraveyardCards(Owner).OfType<Polymerization>().Any();

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner ?? cardPlay.Card?.Owner;
        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (player?.Creature == null || pet == null)
            return;

        Polymerization? poly = YgoMpCombatOrder.FirstCardWhereStable(
            YgoPlayerPiles.GraveyardCards(player),
            c => c is Polymerization) as Polymerization;
        if (poly == null)
            return;

        CardPile? gy = YgoPlayerPiles.Graveyard(player);
        CardPile? hand = YgoPlayerPiles.Hand(player);
        if (gy == null || hand == null || !gy.Cards.Contains(poly))
            return;

        if (!player.Creature.HasPower<RaDoomedPower>())
            await PowerCmd.Apply<RaDoomedPower>(player.Creature, 1m, player.Creature, source);

        decimal doomGain = source.DynamicVars["Mgc"].BaseValue;
        if (doomGain > 0m)
            await PowerCmd.Apply<DoomPower>(player.Creature, doomGain, player.Creature, source);

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
        await CardPileCmd.Add(new[] { poly }, hand, CardPilePosition.Top, poly, false);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Mgc"].BaseValue = 5m;
    }
}
