using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using YgoDuelist.YgoDuelistCode.Cards;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

public sealed class Big_Shield_Gardna : EffectMonsterCard
{
    [SavedProperty]
    public int FortifiedBeastStacks { get; set; }

    protected override bool HasRecklessBlockerKeyword => true;

    public Big_Shield_Gardna()
        : base(
            cost: 1,
            type: CardType.Attack,
            rarity: CardRarity.Uncommon,
            target: TargetType.AnyEnemy,
            duelMonsterLevel: 4,
            duelMonsterAttribute: DuelMonsterAttribute.Earth,
            baseAtk: 1,
            baseDef: 26,
            baseMgc: 0,
            duelMonsterRace: DuelMonsterRace.Warrior)
    {
    }
    // Dictates the card pack tags this card will be included in.
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Earth | YgoCardPackTags.Warrior;

    // You will always see bundled cards when RNG rolls this card, but not the other way around.
    //public override Type[] BundledCards => new[]
    //{
    //    typeof(This_Card),
    //    typeof(Another_Bundled_Card)
    //};

    // You will see these related cards more often with this card in your deck or side deck.
    public override Type[] RelatedCards => new[]
    {
        typeof(Big_Shield_Gardna),
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            yield return HoverTipFactory.FromPower<FortifiedBeastPower>();
        }
    }

    public override int GetFortifiedBeastsBonusMaxHp(Player player) => Math.Max(0, FortifiedBeastStacks);

    public override bool ReconcileDieForYouChecksumForPet(Creature pet, Player player)
    {
        MonsterCommandState st = MonsterCommandRegistry.GetOrCreate(pet);
        st.DieForYouForced = true;
        st.DieForYouEnabled = true;

        if (!pet.HasPower<DieForYouPower>())
        {
            MonsterCommandRegistry.ApplyDieForYouSyncForChecksum(pet, player.Creature, this);
            GD.Print(
                $"[YgoDuelist][MP][DieForYou] Reconciled Big_Shield_Gardna forced DieForYouPower (playerNetId={player.NetId} petCombatId={pet.CombatId})");
        }

        return true;
    }

    public override async Task OnAfterSummonPipelineAsync(Player player, PlayerChoiceContext ctx, Creature pet, bool canAttackThisTurn)
    {
        await MonsterCommandRegistry.SetDieForYouForcedAsync(pet, true, player, this);
        NCombatRoom.Instance?.GetCreatureNode(pet)?.TrackBlockStatus(player.Creature);

        if (FortifiedBeastStacks > 0)
            await PowerCmd.Apply<FortifiedBeastPower>(pet, FortifiedBeastStacks, player.Creature, this);
    }

    protected override async Task OnAfterGainBlockFromCombatActionAsync(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        int blockGranted)
    {
        if (Owner?.Creature == null || Owner.PlayerCombatState == null)
            return;

        Creature? pet = FindFieldPet();
        if (pet == null || !pet.IsAlive)
            return;

        FortifiedBeastStacks++;
        await PowerCmd.Apply<FortifiedBeastPower>(pet, 1m, Owner.Creature, this);
        await FortifiedBeastsDuelMonsterHp.SyncPetFromCardAsync(pet, this, Owner);
    }

    private Creature? FindFieldPet()
    {
        if (Owner?.PlayerCombatState == null)
            return null;

        return YgoMpCombatOrder.FirstPetWhere(
            Owner.PlayerCombatState,
            p => DuelMonsterFieldRegistry.HasSourceCard(p, this));
    }
}
