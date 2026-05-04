using YgoDuelist.YgoDuelistCode.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YgoDuelist.YgoDuelistCode.Cards.Core;
using YgoDuelist.YgoDuelistCode.Models;
using YgoDuelist.YgoDuelistCode.Powers;
using YgoDuelist.YgoDuelistCode.Relics;
using YgoDuelist.YgoDuelistCode.Services;

namespace YgoDuelist.YgoDuelistCode.Cards.Monster.Done.Effect;

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
    public override YgoCardPackTags PackTags => YgoCardPackTags.MultiplayerSafe | YgoCardPackTags.Starter | YgoCardPackTags.Heal;
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (IHoverTip t in base.ExtraHoverTips)
                yield return t;
            if (!IsUpgradedOrPreviewActive)
                yield return HoverTipFactory.FromPower<UpkeepLifeLossPower>();
        }
    }

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
        if (!IsUpgraded)
            await PowerCmd.Apply<UpkeepLifeLossPower>(pet, 1m, player.Creature, this);
    }

    /// <summary>Upgraded: no <see cref="UpkeepLifeLossPower"/>; heal still runs once per turn (same timing as former GraveyardRelic upkeep).</summary>
    public override async Task OnGraveyardRelicOwnerTurnStartForFieldPetAsync(
        PlayerChoiceContext ctx,
        Player player,
        Creature pet,
        GraveyardRelic relic)
    {
        if (!IsUpgraded)
            return;
        string key = $"CURE_MERMAID_{pet.CombatId}";
        if (!relic.TryConsumeAnnual(key))
            return;
        if (player.Creature != null)
            await CreatureCmd.Heal(player.Creature, 1m);
    }
}
