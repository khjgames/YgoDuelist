using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Piles;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

/// <summary>When sent from the Field to the Graveyard: gain 1 Conduit and heal all FIRE duel monsters {Mgc} HP.</summary>
public sealed class Molten_Zombie : EffectMonsterCard
{
    public Molten_Zombie()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Fire,
            baseAtk: 16,
            baseDef: 4,
            baseMgc: 2,
            duelMonsterRace: DuelMonsterRace.Pyro)
    {
    }

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Fire | YgoCardPackTags.Zombie;

    public override Type[] RelatedCards => new[] { typeof(Molten_Zombie) };

    public override void OnMovedToGraveyardFromHandOrField(PileType from)
    {
        if (from != MonsterPile.CustomType)
            return;
        Player? player = Owner;
        if (player?.Creature == null)
            return;
        TaskHelper.RunSafely(RunFromFieldToGraveyardAsync(player));
    }

    private async Task RunFromFieldToGraveyardAsync(Player player)
    {
        await PlayerCmd.GainStars(1, player);
        decimal heal = DynamicVars["Mgc"].BaseValue;
        if (heal <= 0m || player.PlayerCombatState == null || player.Creature == null)
            return;

        foreach (Creature pet in YgoMpCombatOrder.PetsSnapshotOrderedByCombatId(player.PlayerCombatState))
        {
            if (!pet.IsAlive)
                continue;
            if (DuelMonsterFieldRegistry.GetSourceMonster<BaseMonsterCard>(pet) is not BaseMonsterCard src)
                continue;
            if (src.DuelMonsterAttribute != DuelMonsterAttribute.Fire)
                continue;
            await CreatureCmd.Heal(pet, heal);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["Mgc"].BaseValue = 5m;
    }
}
