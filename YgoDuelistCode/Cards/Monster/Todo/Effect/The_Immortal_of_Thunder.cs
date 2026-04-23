using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Piles;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

/// <summary>FLIP: heal Mgc. When sent from the field to the Graveyard, take Mgc2 unblockable damage.</summary>
public sealed class The_Immortal_of_Thunder : EffectMonsterCard, IMonsterFlipEffect
{
    public The_Immortal_of_Thunder()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterAttribute.Light,
            baseAtk: 15,
            baseDef: 13,
            baseMgc: 3,
            duelMonsterRace: global::YgoDuelist.YgoDuelistCode.Models.DuelMonsterRace.Thunder)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Light | YgoCardPackTags.Heal;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (DynamicVar v in base.CanonicalVars)
                yield return v;
            yield return new DynamicVar("Mgc2", 5m);
        }
    }

    public async Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self)
    {
        if (self is not The_Immortal_of_Thunder || Owner?.Creature == null)
            return;

        decimal heal = DynamicVars["Mgc"].BaseValue;
        if (heal > 0m)
            await CreatureCmd.Heal(Owner.Creature, heal);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 4m;
        DynamicVars["Mgc2"].BaseValue = 3m;
    }

    public override void OnAfterPileMoveCompleted(Player? player, PileType? from, PileType newPileType)
    {
        if (player == null || from != MonsterPile.CustomType || newPileType != GraveyardPile.CustomType)
            return;
        TaskHelper.RunSafely(DealFieldToGraveyardDamageAsync(player));
    }

    private async Task DealFieldToGraveyardDamageAsync(Player player)
    {
        if (player.Creature?.CombatState == null)
            return;

        decimal damage = DynamicVars["Mgc2"].BaseValue;
        if (damage <= 0m)
            return;

        var ctx = YgoDuelist.YgoDuelistCode.Services.YgoChoiceContexts.Blocking();
        await CreatureCmd.Damage(
            ctx,
            player.Creature,
            damage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            player.Creature,
            this);
    }
}
