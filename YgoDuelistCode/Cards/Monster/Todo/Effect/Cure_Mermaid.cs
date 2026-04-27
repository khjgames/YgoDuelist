using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Cure_Mermaid : EffectMonsterCard
{
    protected override bool HasRecklessBlockerKeyword => true;

    public Cure_Mermaid()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Water,
            baseAtk: 15,
            baseDef: 8,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fish,
            duelMonsterAttackPlayEnergyOverride: 2,
            duelMonsterDefensePlayEnergyOverride: 1)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.Starter | YgoCardPackTags.Heal;
    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Cure_Mermaid),
    };

    protected override void OnUpgrade() => base.OnUpgrade();

    public override bool ReconcileDieForYouChecksumForPet(Creature pet, Player player)
    {
        MonsterCommandState st = MonsterCommandRegistry.GetOrCreate(pet);
        st.DieForYouForced = true;
        st.DieForYouEnabled = true;

        if (!pet.HasPower<DieForYouPower>())
        {
            MonsterCommandRegistry.ApplyDieForYouSyncForChecksum(pet, player.Creature, this);
            GD.Print(
                $"[YgoDuelist][MP][DieForYou] Reconciled Cure_Mermaid forced DieForYouPower (playerNetId={player.NetId} petCombatId={pet.CombatId})");
        }

        return true;
    }

    public override async Task OnAfterSummonPipelineAsync(Player player, PlayerChoiceContext ctx, Creature pet, bool canAttackThisTurn)
    {
        await MonsterCommandRegistry.SetDieForYouForcedAsync(pet, true, player, this);
        NCombatRoom.Instance?.GetCreatureNode(pet)?.TrackBlockStatus(player.Creature);
    }

    public override async Task OnGraveyardRelicOwnerTurnStartForFieldPetAsync(
        PlayerChoiceContext ctx,
        Player player,
        Creature pet,
        GraveyardRelic relic)
    {
        string key = $"CURE_MERMAID_{pet.CombatId}";
        if (!relic.TryConsumeAnnual(key))
            return;
        decimal maintenance = IsUpgraded ? 0m : 1m;
        if (maintenance > 0)
            await CreatureCmd.Damage(
                ctx,
                pet,
                maintenance,
                ValueProp.Unblockable | ValueProp.Unpowered,
                player.Creature,
                this);
        if (player.Creature != null)
            await CreatureCmd.Heal(player.Creature, 1m);
    }
}
