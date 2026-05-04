using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

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

    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Dark | YgoCardPackTags.Fiend;

    public override Type[] RelatedCards => new[] { typeof(Spear_Cretin) };

    public Task OnFlippedFaceUpAsync(PlayerChoiceContext choiceContext, AbstractMonsterCard self) =>
        Task.CompletedTask;

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
                $"[YgoDuelist][MP][DieForYou] Reconciled Spear_Cretin forced DieForYouPower (playerNetId={player.NetId} petCombatId={pet.CombatId})");
        }

        return true;
    }

    public override async Task OnAfterSummonPipelineAsync(Player player, PlayerChoiceContext ctx, Creature pet, bool canAttackThisTurn)
    {
        await MonsterCommandRegistry.SetDieForYouForcedAsync(pet, true, player, this);
        NCombatRoom.Instance?.GetCreatureNode(pet)?.TrackBlockStatus(player.Creature);
    }
}
