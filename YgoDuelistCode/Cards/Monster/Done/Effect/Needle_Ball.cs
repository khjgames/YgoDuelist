using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>0-cost Activate: take <c>Mgc</c> blockable damage; inflict <c>Mgc2</c> Blight on target enemy.</summary>
public sealed class Needle_Ball : EffectMonsterCard, IMonsterActivatedEffect
{
    public override int AttackPortionCount => 4;

    public Needle_Ball()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 7,
            baseDef: 7,
            baseMgc: 11,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<BlightPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in base.CanonicalVars)
                yield return v;
            yield return new DynamicVar("Mgc2", 16m);
        }
    }

    public int ActivatedEffectEnergyCost => 0;
    public CardType ActivatedEffectCardType => CardType.Attack;
    public TargetType ActivatedEffectTarget => TargetType.AnyEnemy;
    public string ActivatedEffectDescriptionLocKey => "YGODUELIST-NEEDLE_BALL.activated_effect.description";

    public async Task OnActivatedEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay, NormalMonsterCard source)
    {
        Player? player = source.Owner;
        if (player?.Creature == null)
            return;

        Creature? pet = MonsterActivatedEffectRuntime.FindPetForSourceMonster(source, player);
        if (pet == null)
            return;

        decimal selfDmg = source.DynamicVars["Mgc"].BaseValue;
        if (selfDmg > 0m)
        {
            // Move|Unpowered: life cost is still blockable as a move, but not a "powered attack", so
            // DieForYouPower does not redirect it. Redirected powered hits use Hook listener order (allies/pets),
            // which can differ between host and client and desync checksums (e.g. co-op Needle Ball + multiple pets).
            await CreatureCmd.Damage(
                choiceContext,
                player.Creature,
                selfDmg,
                ValueProp.Move | ValueProp.Unpowered,
                dealer: null,
                cardSource: source);
        }

        Creature? target = cardPlay.Target;
        if (target == null || !target.IsAlive)
            return;

        int blight = (int)source.DynamicVars["Mgc2"].BaseValue;
        if (blight > 0)
            await PowerCmd.Apply<BlightPower>(target, blight, player.Creature, source);

        MonsterCommandRegistry.SetHasUsedActivatedEffectThisTurn(pet, true);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 8m;
        DynamicVars["Mgc2"].BaseValue = 21m;
    }
}
