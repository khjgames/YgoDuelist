using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Todo.Effect;

public sealed class Spear_Cretin : EffectMonsterCard, IMonsterFlipEffect
{
    protected override bool HasRecklessBlockerKeyword => true;

    public Spear_Cretin()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 2,
            duelMonsterAttribute: DuelMonsterAttribute.Dark,
            baseAtk: 5,
            baseDef: 5,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Fiend)
    {
    }

    public override YgoCardPackTags PackTags =>
        YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Spear_Cretin) };

    public Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self) =>
        Task.CompletedTask;

    protected override void OnUpgrade() => base.OnUpgrade();

    public override bool ReconcileDieForYouChecksumForPet(Creature pet, Player player)
    {
        if (!pet.HasPower<DieForYouPower>())
        {
            MonsterCommandState st = MonsterCommandRegistry.GetOrCreate(pet);
            st.DieForYouForced = true;
            st.DieForYouEnabled = true;
            MonsterCommandRegistry.ApplyDieForYouSyncForChecksum(pet, player.Creature, this);
            GD.Print(
                $"[YgoDuelist][MP][DieForYou] Reconciled Spear_Cretin forced DieForYouPower (playerNetId={player.NetId} petCombatId={pet.CombatId})");
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
        string key = $"SPEAR_CRETIN_{pet.CombatId}";
        if (!relic.TryConsumeAnnual(key))
            return;
        decimal maintenance = IsUpgraded ? 0m : 1m;
        if (maintenance > 0m)
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
